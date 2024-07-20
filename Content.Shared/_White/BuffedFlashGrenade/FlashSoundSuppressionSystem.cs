using Content.Shared.Inventory;
using Content.Shared.Stunnable;

namespace Content.Shared._White.BuffedFlashGrenade;

public sealed class FlashSoundSuppressionSystem : EntitySystem
{
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FlashSoundSuppressionComponent, InventoryRelayedEvent<GetFlashbangedEvent>>(
            OnGetFlashbanged);
    }

    private void OnGetFlashbanged(Entity<FlashSoundSuppressionComponent> ent,
        ref InventoryRelayedEvent<GetFlashbangedEvent> args)
    {
        args.Args.Protected = true;
    }

    public void Stun(EntityUid target, float duration)
    {
        if (HasComp<FlashSoundSuppressionComponent>(target))
            return;

        var ev = new GetFlashbangedEvent();
        RaiseLocalEvent(target, ev);
        if (ev.Protected)
            return;

        _stunSystem.TryParalyze(target, TimeSpan.FromSeconds(duration / 1000f), true);
    }
}

public sealed class GetFlashbangedEvent : EntityEventArgs, IInventoryRelayEvent
{
    public bool Protected;

    public SlotFlags TargetSlots => SlotFlags.EARS | SlotFlags.HEAD;
}
