using Content.Server.Flash.Components;
using Content.Server.Store.Components;
using Content.Shared._White.Overlays;
using Content.Shared.Changeling;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Implants.Components;
using Content.Shared.Inventory;

namespace Content.Server.Changeling;

public sealed partial class ChangelingSystem
{
    private void InitializeShop()
    {
        SubscribeLocalEvent<SubdermalImplantComponent, ChangelingShopActionEvent>(OnShop);
        SubscribeLocalEvent<ChangelingComponent, ChangelingRefundEvent>(OnChangelingRefund);
        SubscribeLocalEvent<DeleteOnChangelingRefundComponent, InventoryRelayedEvent<ChangelingRefundEvent>>(OnRefund);
    }

    private void OnRefund(Entity<DeleteOnChangelingRefundComponent> ent,
        ref InventoryRelayedEvent<ChangelingRefundEvent> args)
    {
        QueueDel(ent);
    }

    private void OnChangelingRefund(Entity<ChangelingComponent> ent, ref ChangelingRefundEvent args)
    {
        RemComp<EyeProtectionComponent>(ent);
        RemComp<FlashImmunityComponent>(ent);
        RemComp<TemporaryNightVisionComponent>(ent);
        RemComp<TemporaryThermalVisionComponent>(ent);
        RemComp<VoidAdaptationComponent>(ent);

        foreach (var hand in _handsSystem.EnumerateHands(ent))
        {
            if (hand.HeldEntity != null && HasComp<DeleteOnChangelingRefundComponent>(hand.HeldEntity.Value))
                QueueDel(hand.HeldEntity.Value);
        }

        if (!TryComp(args.Store, out StoreComponent? storeComponent))
            return;

        _storeSystem.DisableRefund(args.Store, storeComponent);
    }

    private void OnShop(EntityUid uid, SubdermalImplantComponent component, ChangelingShopActionEvent args)
    {
        if(!TryComp<StoreComponent>(uid, out var store))
            return;

        _storeSystem.ToggleUi(args.Performer, uid, store);
    }
}
