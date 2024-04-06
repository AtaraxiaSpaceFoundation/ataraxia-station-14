using Robust.Shared.GameStates;

namespace Content.Shared._White.Telescope;

[RegisterComponent, NetworkedComponent]
public sealed partial class TelescopeComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MaxLength = 670f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MinLength = 100f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Divisor = 60f;

    [ViewVariables]
    public EntityUid? LastHoldingEntity;
}
