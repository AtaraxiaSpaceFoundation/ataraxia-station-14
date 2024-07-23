using Content.Server.Explosion.EntitySystems;
using Content.Shared.Throwing;

namespace Content.Server._White.Explosion;

public sealed class TriggerOnLandSystem : EntitySystem
{
    [Dependency] private readonly TriggerSystem _timer = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TriggerOnLandComponent, LandEvent>(OnLand);
    }

    private void OnLand(EntityUid uid, TriggerOnLandComponent component, ref LandEvent args)
    {
        _timer.HandleTimerTrigger(uid, null, component.Delay, 1, null, null);
    }
}
