using Content.Server.Power.Components;
using Content.Shared.Lightning;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Content.Shared.Weapons.Ranged.Components;

namespace Content.Server._White.Lighting.Pointlight.Battery;

public sealed class PointLightBatterySystem : SharedLightningSystem
{
    [Dependency] private readonly SharedPointLightSystem _pointLightSystem = default!;
    [Dependency] private readonly SharedPowerCellSystem _cell = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PointLightBatteryComponent, PowerCellChangedEvent>(OnBatteryLoose);
        SubscribeLocalEvent<PointLightBatteryComponent, ChargeChangedEvent>(OnBatteryChargeChanged);
    }

    private void OnBatteryLoose(EntityUid uid, PointLightBatteryComponent component, PowerCellChangedEvent args)
    {
        if (!component.RequireBattery)
            return;

        if (!_pointLightSystem.TryGetLight(uid, out var pointLightComponent))
            return;

        var isBatteryCharged = _cell.HasDrawCharge(uid);
        _pointLightSystem.SetEnabled(uid, isBatteryCharged && !args.Ejected, pointLightComponent);

        RaiseLocalEvent(uid, new PointLightToggleEvent(isBatteryCharged && !args.Ejected), true);
    }

    private void OnBatteryChargeChanged(EntityUid uid, PointLightBatteryComponent component, ChargeChangedEvent args)
    {
        if (!component.RequireBattery)
            return;

        if (!_pointLightSystem.TryGetLight(uid, out var pointLightComponent))
            return;

        var isBatteryCharged = TryComp<ProjectileBatteryAmmoProviderComponent>(uid, out var projectileBattery) && projectileBattery.Shots > 0;
        _pointLightSystem.SetEnabled(uid, isBatteryCharged, pointLightComponent);

        RaiseLocalEvent(uid, new PointLightToggleEvent(isBatteryCharged), true);
    }
}
