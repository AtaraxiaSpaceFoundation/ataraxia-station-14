using Content.Shared._White.Wizard.ScrollSystem;

namespace Content.Server._White.Wizard.Scrolls;

public sealed class ScrollSystem : SharedScrollSystem
{
    protected override void BurnScroll(EntityUid uid) => Del(uid);
}
