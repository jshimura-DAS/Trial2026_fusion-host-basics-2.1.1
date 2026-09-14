using Fusion;
using UnityEngine;

public class Ball : NetworkBehaviour
{
    private const float Speed = 5f;
    private const float LifetimeSeconds = 5f;

    [Networked]
    private TickTimer _life { get; set; }

    public void Init()
    {
        _life = TickTimer.CreateFromSeconds(Runner, LifetimeSeconds);
    }

    public override void FixedUpdateNetwork()
    {
        if (_life.Expired(Runner))
            Runner.Despawn(Object);
        else
            transform.position += Speed * transform.forward * Runner.DeltaTime;
    }
}
