using Content.Shared.Lightning;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;

namespace Content.Server._White.Lighting;

public sealed class PointLightBatterySystem : SharedLightningSystem
{
    [Dependency] private readonly SharedPointLightSystem _pointLightSystem = default!;
    [Dependency] private readonly SharedPowerCellSystem _cell = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PointLightBatteryComponent, PowerCellChangedEvent>(OnBatteryLoose);
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
}
