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
    private float _shootElapsed;

    [Networked]
    private TickTimer _delay { get; set; }

    private TMP_Text _messages;


    [SerializeField]
    public Color _initialColor;

    [SerializeField]
    private Color[] _colors = new Color[] { Color.red, Color.green, Color.blue, Color.cyan, Color.magenta };

    [SerializeField]            
    // パラメータ調整
    float _frequency = 64.0f; // 点滅の速さ（振動数）
    [SerializeField] 
    float _decayRate = 4.0f;  // 収束の速さ（大きいほど早く元の色に戻る）


    private void Awake()
    {
        _cc = GetComponent<NetworkCharacterController>();
        _material = GetComponentInChildren<MeshRenderer>().material;
        _messages = FindFirstObjectByType<TMP_Text>();
        _initialColor = _material.color;
    }
    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
        _initialColor = _colors[Runner.LocalPlayer.PlayerId % _colors.Length] ;
        _material.color = _initialColor;
    }
    public override void Render()
    {
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(SpawnedProjectile):
                    _material.color = Color.yellow;
                    _shootElapsed = 0f;
                    break;
            }
        }
        _shootElapsed += Time.deltaTime;


        // Sin波（-1 〜 1）に減衰率を掛ける
        float envelope = Mathf.Exp(-_shootElapsed * _decayRate); // 1.0 から 0.0 へ徐々に減少
        float sinWave = Mathf.Sin(_shootElapsed * _frequency);   // -1.0 〜 1.0 の振動

        // _initialColor（元の色）を基準に、黄色（Yellow）へ向かって振れて最終的に0（元の色）に収束する
        float t = Mathf.Clamp01(sinWave * envelope);

        _material.color = Color.Lerp(_initialColor, Color.yellow, t);
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