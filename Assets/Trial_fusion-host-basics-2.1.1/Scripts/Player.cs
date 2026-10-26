using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : NetworkBehaviour
{
    private const float MoveSpeed = 5f;
    private const float BallSpeed = 10f;
    private const float ShootDelay = 0.5f;
    private Material _material;
    [Networked]
    public bool SpawnedProjectile { get; set; }
    private ChangeDetector _changeDetector;

    [SerializeField]
    private Ball _prefabBall;
    [SerializeField]
    private PhysxBall _prefabPhysxBall;

    private NetworkCharacterController _cc;
    private Vector3 _forward = Vector3.forward;

    [Networked]
    private TickTimer _delay { get; set; }

    private TMP_Text _messages;


    private void Awake()
    {
        _cc = GetComponent<NetworkCharacterController>();
        _material = GetComponentInChildren<MeshRenderer>().material;
        _messages = FindFirstObjectByType<TMP_Text>();
    }
    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
    }
    public override void Render()
    {
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(SpawnedProjectile):
                    _material.color = Color.white;
                    break;
            }
        }
        _material.color = Color.Lerp(_material.color, Color.blue, Time.deltaTime);
    }
    public void Update()
    {
        if (Object.HasInputAuthority )
        {
            if(Keyboard.current?.rKey.isPressed ?? false)
            {
                RPC_SendMessage("Hey Mate!");
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            data.Direction.Normalize();
            _cc.Move(MoveSpeed * data.Direction * Runner.DeltaTime);

            if (data.Direction.sqrMagnitude > 0)
                _forward = data.Direction;

            if (HasStateAuthority && _delay.ExpiredOrNotRunning(Runner))
            {
                if (data.Buttons.IsSet(NetworkInputData.MouseButton0))
                {
                    _delay = TickTimer.CreateFromSeconds(Runner, ShootDelay);
                    Runner.Spawn(
                        _prefabBall,
                        transform.position + _forward,
                        Quaternion.LookRotation(_forward),
                        Object.InputAuthority,
                        (runner, o) => { o.GetComponent<Ball>().Init(); }
                    );
                    SpawnedProjectile = !SpawnedProjectile;
                }
                if (data.Buttons.IsSet(NetworkInputData.MouseButton1))
                {
                    _delay = TickTimer.CreateFromSeconds(Runner, ShootDelay);
                    Runner.Spawn(
                        _prefabPhysxBall,
                        transform.position + _forward,
                        Quaternion.LookRotation(_forward),
                        Object.InputAuthority,
                        (runner, o) => { o.GetComponent<PhysxBall>().Init(BallSpeed * _forward); }
                    );
                    SpawnedProjectile = !SpawnedProjectile;
                }
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
    public void RPC_SendMessage(string message, RpcInfo info = default)
    {
        RPC_RelayMessage(message, info.Source);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All, HostMode = RpcHostMode.SourceIsServer)]
    public void RPC_RelayMessage(string message, PlayerRef messageSource)
    {
        if (_messages == null)
            _messages = UnityEngine.Object.FindFirstObjectByType<TMP_Text>();

        if (messageSource == Runner.LocalPlayer)
        {
            message = $"You said: {message}\n";
        }
        else
        {
            message = $"Some other player said: {message}\n";
        }

        _messages.text += message;
    }
}