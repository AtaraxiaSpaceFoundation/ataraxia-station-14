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
        args.Args.MaxRange = MathF.Min(args.Args.MaxRange, ent.Comp.MaxRange);
    }

    public void Stun(EntityUid target, float duration, float distance, float range)
    {
        if (TryComp<FlashSoundSuppressionComponent>(target, out var suppression))
            range = MathF.Min(range, suppression.MaxRange);

        var ev = new GetFlashbangedEvent();
        ev.MaxRange = range;
        RaiseLocalEvent(target, ev);
        range = MathF.Min(range, ev.MaxRange);

        if (range <= 0f)
            return;
        if (distance < 0f)
            distance = 0f;
        if (distance > range)
            return;

        var stunTime = float.Lerp(duration, 0f, distance / range);
        if (stunTime <= 0f)
            return;

        _stunSystem.TryParalyze(target, TimeSpan.FromSeconds(stunTime / 1000f), true);
    }
}

public sealed class GetFlashbangedEvent : EntityEventArgs, IInventoryRelayEvent
{
    public float MaxRange = 7f;

    public SlotFlags TargetSlots => SlotFlags.EARS | SlotFlags.HEAD;
}
