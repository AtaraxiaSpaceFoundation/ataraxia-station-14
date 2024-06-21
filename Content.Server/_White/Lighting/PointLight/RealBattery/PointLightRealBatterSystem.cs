using Content.Server.Power.Components;
using Content.Shared.PowerCell;
using Content.Shared.Rounding;

namespace Content.Server._White.Lighting.PointLight.RealBattery;

public sealed class PointLightRealBatterySystem : EntitySystem
{
    [Dependency] private readonly SharedPointLightSystem _pointLightSystem = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PointLightRealBatteryComponent, ChargeChangedEvent>(OnChargeChanged);
        SubscribeLocalEvent<PointLightRealBatteryComponent, ComponentInit>(OnComponentInit);
    }

    public void ToggleLight(EntityUid uid, string hex, bool enable = true)
    {
        if (!_pointLightSystem.TryGetLight(uid, out var pointLightComponent))
            return;

        if (enable)
        {
            var color = Color.FromHex(hex);
            _pointLightSystem.SetColor(uid, color, pointLightComponent);
        }

        _pointLightSystem.SetEnabled(uid, enable, pointLightComponent);

        RaiseLocalEvent(uid, new PointLightToggleEvent(enable), true);
    }

    public void OnComponentInit(EntityUid uid, PointLightRealBatteryComponent component, ComponentInit args)
    {
        if (!TryComp<BatteryComponent>(uid, out var battery))
            return;

        var ev = new ChargeChangedEvent(battery.CurrentCharge, battery.MaxCharge);
        RaiseLocalEvent(uid, ref ev);
    }
    public void OnChargeChanged(EntityUid uid, PointLightRealBatteryComponent component, ChargeChangedEvent args)
    {
        var frac = args.Charge / args.MaxCharge;
        var level = (byte) ContentHelpers.RoundToNearestLevels(frac, 1, PowerCellComponent.PowerCellVisualsLevels);

        switch (level)
        {
            case 2:
                ToggleLight(uid, component.GreenColor);
                break;

            case 1:
                ToggleLight(uid, component.YellowColor);
                break;

            case 0:
                ToggleLight(uid, string.Empty, false);
                break;
        }

    }

}
