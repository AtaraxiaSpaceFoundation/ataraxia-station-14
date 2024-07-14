using Content.Server.Popups;
using Content.Server.Power.EntitySystems;
using Content.Server.PowerCell;
using Content.Server.SurveillanceCamera;
using Content.Shared.Examine;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Item;
using Content.Shared.Actions;
using Content.Shared.PowerCell.Components;
using Content.Shared._White.SurveillanceCamera;
using Content.Shared.IdentityManagement;
using Content.Shared.Toggleable;
using Robust.Shared.Player;

namespace Content.Server._White.SurveillanceCamera;

public sealed class SurveillanceBodyCameraSystem : EntitySystem
{
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SurveillanceCameraSystem _surveillanceCameras = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly ClothingSystem _clothing = default!;
    [Dependency] private readonly BatterySystem _battery = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SurveillanceBodyCameraComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<SurveillanceBodyCameraComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<SurveillanceBodyCameraComponent, ToggleBodyCameraEvent>(OnToggleAction);
        SubscribeLocalEvent<SurveillanceBodyCameraComponent, PowerCellChangedEvent>(OnPowerCellChanged);
        SubscribeLocalEvent<SurveillanceBodyCameraComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<SurveillanceBodyCameraComponent, ComponentInit>(OnInit);
    }

    private void OnStartup(EntityUid uid, SurveillanceBodyCameraComponent component, ComponentStartup args)
    {
        EnsureComp(uid, out SurveillanceCameraComponent surComp);
        _surveillanceCameras.UpdateSetupInterface(uid, surComp);
    }

    private void OnGetActions(EntityUid uid, SurveillanceBodyCameraComponent component, GetItemActionsEvent args)
    {
        args.AddAction(ref component.ToggleActionEntity, component.ToggleAction);
    }

    private void OnToggleAction(EntityUid uid, SurveillanceBodyCameraComponent component, ToggleBodyCameraEvent args)
    {
        if (!TryComp<SurveillanceCameraComponent>(uid, out var surComp))
            return;

        if (!_powerCell.TryGetBatteryFromSlot(uid, out var battery))
            return;

        _surveillanceCameras.SetActive(uid, battery.CurrentCharge > component.Wattage && !surComp.Active, surComp);
        AppearanceChange(uid, surComp.Active);

        var message = Loc.GetString(surComp.Active ? "surveillance-body-camera-on" : "surveillance-body-camera-off",
            ("item", Identity.Entity(uid, EntityManager)));
        _popup.PopupEntity(message, uid, Filter.PvsExcept(uid, entityManager: EntityManager), true);
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

            if (_battery.TryUseCharge(uid, cam.Wattage * frameTime, battery))
                continue;

            var message = Loc.GetString("surveillance-body-camera-off",
                    ("item", Identity.Entity(uid, EntityManager)));
            _popup.PopupEntity(message, uid, Filter.PvsExcept(uid, entityManager: EntityManager), true);

            _surveillanceCameras.SetActive(uid, false, surComp);
            AppearanceChange(uid, surComp.Active);
        }
    }

    private void OnPowerCellChanged(EntityUid uid, SurveillanceBodyCameraComponent comp, PowerCellChangedEvent args)
    {
        if (!TryComp<SurveillanceCameraComponent>(uid, out var surComp))
            return;

        if (!args.Ejected)
            return;

        if (surComp.Active)
        {
            var message = Loc.GetString("surveillance-body-camera-off",
                ("item", Identity.Entity(uid, EntityManager)));
            _popup.PopupEntity(message, uid, Filter.PvsExcept(uid, entityManager: EntityManager), true);
        }

        _surveillanceCameras.SetActive(uid, false, surComp);
        AppearanceChange(uid, surComp.Active);
    }

    public void OnExamine(EntityUid uid, SurveillanceBodyCameraComponent comp, ExaminedEvent args)
    {
        if (!TryComp<SurveillanceCameraComponent>(uid, out var surComp))
            return;

        if (!args.IsInDetailsRange)
            return;

        var message = Loc.GetString(surComp.Active ? "surveillance-body-camera-on" : "surveillance-body-camera-off",
            ("item", Identity.Entity(uid, EntityManager)));
        args.PushMarkup(message);
    }

    public void AppearanceChange(EntityUid uid, bool isActive)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance) ||
            !TryComp<ItemComponent>(uid, out var item))
            return;

        _item.SetHeldPrefix(uid, isActive ? "on" : "off", false, item);
        _clothing.SetEquippedPrefix(uid, isActive ? null : "off");
        _appearance.SetData(uid, ToggleVisuals.Toggled, isActive, appearance);
    }
}
