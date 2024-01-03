using Content.Server.Popups;
using Content.Server.PowerCell;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.PowerCell.Components;
using Content.Shared.Toggleable;

namespace Content.Server.SurveillanceCamera;

/// <summary>
/// This handles the bodycamera all itself. Activation, examine,init, powercell stuff.
/// </summary>
public sealed class SurveillanceBodyCameraSystem : EntitySystem
{
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SurveillanceCameraSystem _surveillanceCameras = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly ClothingSystem _clothing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SurveillanceBodyCameraComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<SurveillanceBodyCameraComponent, PowerCellChangedEvent>(OnPowerCellChanged);
        SubscribeLocalEvent<SurveillanceBodyCameraComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<SurveillanceBodyCameraComponent, ComponentInit>(OnInit);
    }

    public void OnInit(EntityUid uid, SurveillanceBodyCameraComponent comp, ComponentInit args)
    {

        if (!TryComp<SurveillanceCameraComponent>(uid, out var surComp))
            return;

        _surveillanceCameras.SetActive(uid, false, surComp);
        surComp.NetworkSet = true;
        AppearanceChange(uid, surComp.Active);
    }
    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<SurveillanceBodyCameraComponent>();
        while (query.MoveNext(out var uid, out var cam))
        {
            if (!_powerCell.TryGetBatteryFromSlot(uid, out var battery))
                continue;

            if (!TryComp<SurveillanceCameraComponent>(uid, out var surComp))
                continue;

            if (!surComp.Active)
                continue;

            if (!battery.TryUseCharge(cam.Wattage * frameTime))
            {
                _surveillanceCameras.SetActive(uid, false, surComp);
                AppearanceChange(uid, surComp.Active);
            }
        }
    }
    public void OnActivate(EntityUid uid, SurveillanceBodyCameraComponent comp, ActivateInWorldEvent args)
    {
        if (!TryComp<SurveillanceCameraComponent>(uid, out var surComp))
            return;

        if (!_powerCell.TryGetBatteryFromSlot(uid, out var battery))
            return;

        _surveillanceCameras.SetActive(uid, battery.CurrentCharge > comp.Wattage && !surComp.Active, surComp);
        AppearanceChange(uid, surComp.Active);

        var message = Loc.GetString(surComp.Active ? "surveillance-body-camera-on" : "surveillance-body-camera-off");
        _popup.PopupEntity(message, args.User, args.User);
        args.Handled = true;
    }

    public void OnPowerCellChanged(EntityUid uid, SurveillanceBodyCameraComponent comp, PowerCellChangedEvent args)
    {
        if (!TryComp<SurveillanceCameraComponent>(uid, out var surComp))
            return;

        if (args.Ejected)
        {
            _surveillanceCameras.SetActive(uid, false, surComp);
            AppearanceChange(uid, surComp.Active);
        }
    }

    public void OnExamine(EntityUid uid, SurveillanceBodyCameraComponent comp, ExaminedEvent args)
    {
        if (!TryComp<SurveillanceCameraComponent>(uid, out var surComp))
            return;

        if (args.IsInDetailsRange)
        {
            var message =
                Loc.GetString(surComp.Active ? "surveillance-body-camera-on" : "surveillance-body-camera-off");
            args.PushMarkup(message);
        }
    }

    public void AppearanceChange(EntityUid uid, Boolean isActive)
    {
        if (TryComp<AppearanceComponent>(uid, out var appearance) &&
            TryComp<ItemComponent>(uid, out var item))
        {
            _item.SetHeldPrefix(uid, isActive ? "on" : "off", false, item);
            _clothing.SetEquippedPrefix(uid, isActive ? null : "off");
            _appearance.SetData(uid, ToggleVisuals.Toggled, isActive, appearance);
        }
    }
}
