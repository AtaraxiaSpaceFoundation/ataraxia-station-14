using Content.Shared.Armor;
using Content.Shared.Inventory;

namespace Content.Shared._White.StaminaProtection;

public sealed class StaminaProtectionSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArmorComponent, InventoryRelayedEvent<StaminaDamageModifyEvent>>(OnDamageModify);
    }

    private void OnDamageModify(Entity<ArmorComponent> ent, ref InventoryRelayedEvent<StaminaDamageModifyEvent> args)
    {
        var modifiers = ent.Comp.Modifiers;

        if (modifiers.FlatReduction.TryGetValue("Blunt", out var flat))
            args.Args.Damage = MathF.Max(0f, args.Args.Damage - flat);

        if (modifiers.Coefficients.TryGetValue("Blunt", out var coefficient))
            args.Args.Damage *= coefficient / 1.5f;
    }
}

public sealed class StaminaDamageModifyEvent : EntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots => ~SlotFlags.POCKET;

    public float Damage;
}
