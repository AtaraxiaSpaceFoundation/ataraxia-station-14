using Content.Shared.Borer;
using Content.Shared.Mobs;
using Robust.Server.Containers;

namespace Content.Server.Borer;


public sealed class ServerBorerHostSystem : EntitySystem
{
    [Dependency] private ServerBorerSystem _borerSystem = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<BorerHostComponent, MobStateChangedEvent>(OnDamageChanged);
    }

    private void OnDamageChanged(EntityUid uid, BorerHostComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Critical)
        {
            RaiseLocalEvent(uid, new BorerBrainReleaseEvent(), true);
        }
        else if (args.NewMobState == MobState.Dead)
        {
            _borerSystem.GetOut(component.BorerContainer.ContainedEntities[0]);
        }
    }
}
