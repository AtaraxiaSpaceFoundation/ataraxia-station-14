using Content.Shared.Standing.Systems;

namespace Content.Shared._White.Collision;

[RegisterComponent]
public sealed partial class KnockdownOnCollideComponent : Component
{
    [DataField]
    public SharedStandingStateSystem.DropHeldItemsBehavior Behavior =
        SharedStandingStateSystem.DropHeldItemsBehavior.NoDrop;
}
