using Robust.Shared.GameStates;

namespace Content.Shared._White.Telescope;

[RegisterComponent, NetworkedComponent]
public sealed partial class TelescopeComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Divisor = 1f;

    [ViewVariables]
    public EntityUid? LastHoldingEntity;
}
