using Content.Server.Atmos.Components;
using Content.Server.Body.Systems;
using Content.Shared.Mobs;

namespace Content.Server._White.Wizard.Magic.Amaterasu;

public sealed class AmaterasuSystem : EntitySystem
{
    [Dependency] private readonly BodySystem _bodySystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AmaterasuComponent, MobStateChangedEvent>(OnMobState);
    }

    private void OnMobState(EntityUid uid, AmaterasuComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState is MobState.Critical or MobState.Dead)
        {
            if(!TryComp<FlammableComponent>(uid, out var flammable))
                return;

            if (flammable.OnFire)
            {
                _bodySystem.GibBody(uid);
                return;
            }

            RemComp<AmaterasuComponent>(uid);
        }
    }
}
