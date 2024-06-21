using Content.Server.Power.Components;
using Content.Shared._White.Lighting;
using Content.Shared._White.Lighting.PointLight.Airlock;
using Content.Shared.Doors.Components;

namespace Content.Server._White.Lighting.Pointlight.Airlock;

public sealed class PointLightAirlockSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PointLightAirlockComponent, PowerChangedEvent>(OnPowerChanged);
    }

    private void OnPowerChanged(EntityUid uid, PointLightAirlockComponent component, PowerChangedEvent args)
    {
        if (!TryComp<DoorComponent>(uid, out var door))
            return;

        RaiseLocalEvent(uid, new DoorlightsChangedEvent(args.Powered ? door.State : null, args.Powered), true);
    }

}
