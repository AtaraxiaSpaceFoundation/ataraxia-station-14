using Content.Shared.Lock;
using Content.Shared.Storage.Components;

namespace Content.Shared._White.Lighting.PointLight.Locker;

public sealed class PointLightLockerSystem : EntitySystem
{
    [Dependency] private readonly SharedPointLightSystem _pointLightSystem = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PointLightLockerComponent, ComponentInit>(OnComponentInit);

        SubscribeLocalEvent<PointLightLockerComponent, LockToggledEvent>(OnLockToggled);
        SubscribeLocalEvent<PointLightLockerComponent, StorageAfterOpenEvent>(OnStorageAfterOpen);
        SubscribeLocalEvent<PointLightLockerComponent, StorageAfterCloseEvent>(OnStorageAfterClose);
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

    public void OnComponentInit(EntityUid uid, PointLightLockerComponent component, ComponentInit args)
    {
        if (!TryComp<LockComponent>(uid, out var locker))
            return;

        ToggleLight(uid, locker.Locked ? component.RedColor : component.GreenColor, true);
    }

    public void OnLockToggled(EntityUid uid, PointLightLockerComponent component, LockToggledEvent args)
    {
        ToggleLight(uid, args.Locked ? component.RedColor : component.GreenColor, true);
    }

    public void OnStorageAfterOpen(EntityUid uid, PointLightLockerComponent component, StorageAfterOpenEvent args)
    {
        ChangeLightOnDoorToggled(uid, component, true);
    }

    public void OnStorageAfterClose(EntityUid uid, PointLightLockerComponent component, StorageAfterCloseEvent args)
    {
        ChangeLightOnDoorToggled(uid, component, false);
    }

    public void ChangeLightOnDoorToggled(EntityUid uid, PointLightLockerComponent component, bool status)
    {
        if (!_pointLightSystem.TryGetLight(uid, out var pointLightComponent))
            return;

        var factor = status ? 1f : -1f;

        _pointLightSystem.SetEnergy(uid, pointLightComponent.Energy - component.ReduceEnergyOnOpen * factor);
        _pointLightSystem.SetRadius(uid, pointLightComponent.Radius- component.ReduceRadiusOnOpen * factor);

        RaiseLocalEvent(uid, new PointLightToggleEvent(true), true);
    }
}
