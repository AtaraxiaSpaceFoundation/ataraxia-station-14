namespace Content.Server._White.Other.CritSystem;

[RegisterComponent]
public sealed partial class BloodLustComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public float SprintModifier = 1.3f;

    [ViewVariables(VVAccess.ReadWrite)]
    public float WalkModifier = 1.3f;

    [ViewVariables(VVAccess.ReadWrite)]
    public float AttackRateModifier = 1.5f;
}
