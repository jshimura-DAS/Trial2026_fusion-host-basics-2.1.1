using Fusion;
using UnityEngine;

public class PhysxBall : NetworkBehaviour
{
    private const float LifetimeSeconds = 5f;

    [Networked]
    private TickTimer _life { get; set; }

    private Rigidbody _rigidbody;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    public void Init(Vector3 forward)
    {
        _life = TickTimer.CreateFromSeconds(Runner, LifetimeSeconds);
        _rigidbody.linearVelocity = forward;
    }

    public override void FixedUpdateNetwork()
    {
        if (_life.Expired(Runner))
            Runner.Despawn(Object);
    }
}
