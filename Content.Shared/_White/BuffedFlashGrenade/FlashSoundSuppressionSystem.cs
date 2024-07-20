using Content.Shared.Inventory.Events;
using Content.Shared.Stunnable;

namespace Content.Shared._White.BuffedFlashGrenade;

public sealed class FlashSoundSuppressionSystem : EntitySystem
{
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;

    public void Stun(EntityUid target, float duration)
    {
        if (HasComp<FlashSoundSuppressionComponent>(target))
            return;

        _stunSystem.TryParalyze(target, TimeSpan.FromSeconds(duration / 1000f), true);
    }
}
