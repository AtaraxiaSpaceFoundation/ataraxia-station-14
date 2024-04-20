using Content.Server.Emp;
using Content.Shared.Examine;
using Content.Shared.Inventory.Events;
using Content.Shared._White.MagGloves;
using Content.Shared.Popups;
using Robust.Shared.Timing;

namespace Content.Server._White.MagGloves;

/// <summary>
/// This handles...
/// </summary>
public sealed class MagneticGlovesSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<MagneticGlovesComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<MagneticGlovesComponent, GotUnequippedEvent>(OnGotUnequipped);
        SubscribeLocalEvent<MagneticGlovesComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<MagneticGlovesComponent, EmpPulseEvent>(OnEmp);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<MagneticGlovesComponent>();
        while (query.MoveNext(out var uid, out var gloves))
        {
            if (_gameTiming.CurTime > gloves.GlovesLastActivation + gloves.GlovesActiveTime && gloves.Enabled)
            {
                RaiseLocalEvent(uid, new ToggleMagneticGlovesEvent());
            }
        }
    }

    public void OnEmp(EntityUid uid, MagneticGlovesComponent component, EmpPulseEvent args)
    {
        if (component.Enabled)
        {
            RaiseLocalEvent(uid, new ToggleMagneticGlovesEvent());
        }
    }

    public void OnGotUnequipped(EntityUid uid, MagneticGlovesComponent component, GotUnequippedEvent args)
    {
        if (args.Slot == "gloves")
        {
            ToggleGloves(args.Equipee, component, false, uid);
        }

        if (component.Enabled)
        {
            _popup.PopupEntity(Loc.GetString("maggloves-deactivated"), uid, args.Equipee);
            RaiseLocalEvent(uid, new ToggleMagneticGlovesEvent());
        }
    }

    public void OnGotEquipped(EntityUid uid, MagneticGlovesComponent component, GotEquippedEvent args)
    {
        if (args.Slot == "gloves")
        {
            ToggleGloves(args.Equipee, component, true, uid);
        }
    }

    public void ToggleGloves(EntityUid owner, MagneticGlovesComponent component, bool active, EntityUid uid)
    {
        if (!active)
        {
            RemComp<KeepItemsOnFallComponent>(owner);
            if (TryComp<MagneticGlovesAdvancedComponent>(uid, out _))
            {
                RemComp<PreventDisarmComponent>(owner);
                RemComp<PreventStrippingFromHandsAndGlovesComponent>(owner);
            }
        }
        else if (component.Enabled)
        {
            EnsureComp<KeepItemsOnFallComponent>(owner);
            if (TryComp<MagneticGlovesAdvancedComponent>(uid, out _))
            {
                EnsureComp<PreventDisarmComponent>(owner);
                EnsureComp<PreventStrippingFromHandsAndGlovesComponent>(owner);
            }
        }
    }

    public void OnExamined(EntityUid uid, MagneticGlovesComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var message = Loc.GetString("maggloves-ready-in") + " " +
            component.GlovesReadyAt.Subtract(_gameTiming.CurTime).TotalSeconds.ToString("0");

        if (component.GlovesReadyAt < _gameTiming.CurTime)
        {
            message = Loc.GetString("maggloves-ready");
        }

        if (component.Enabled)
        {
            message = Loc.GetString("maggloves-enabled-till") + " " + (component.GlovesLastActivation
                .Add(component.GlovesActiveTime).Subtract(_gameTiming.CurTime).TotalSeconds.ToString("0"));
        }

        args.PushMarkup(message);
    }
}