using Content.Shared.Ghost;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared._White.Cult.Components;
using Content.Shared.Inventory.Events;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared._White.Cult.Systems;

public sealed class CultItemSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultItemComponent, GettingPickedUpAttemptEvent>(OnHandPickUp);
        SubscribeLocalEvent<CultItemComponent, BeingEquippedAttemptEvent>(OnEquipAttempt);
        SubscribeLocalEvent<CultItemComponent, AttemptMeleeEvent>(OnMeleeAttempt);
    }

    private void OnEquipAttempt(EntityUid uid, CultItemComponent component, BeingEquippedAttemptEvent args)
    {
        if (CanUse(args.Equipee))
            return;

        args.Cancel();
        _popupSystem.PopupClient(Loc.GetString("cult-item-component-equip-fail"), uid, args.Equipee);
    }

    private void OnMeleeAttempt(Entity<CultItemComponent> ent, ref AttemptMeleeEvent args)
    {
        if (CanUse(args.User))
            return;

        args.Cancelled = true;
        args.Message = Loc.GetString("cult-item-component-attack-fail");
    }

    private void OnHandPickUp(EntityUid uid, CultItemComponent component, GettingPickedUpAttemptEvent args)
    {
        if (component.CanPickUp || CanUse(args.User))
            return;

        args.Cancel();
        _transform.AttachToGridOrMap(uid);
        _popupSystem.PopupClient(Loc.GetString("cult-item-component-pickup-fail", ("name", Name(uid))), uid, args.User);
    }

    private bool CanUse(EntityUid? uid)
    {
        return HasComp<CultistComponent>(uid) || HasComp<GhostComponent>(uid);
    }
}
