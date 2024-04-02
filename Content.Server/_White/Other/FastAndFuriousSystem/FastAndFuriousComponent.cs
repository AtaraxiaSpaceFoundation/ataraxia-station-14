namespace Content.Server._White.Other.FastAndFuriousSystem;

[RegisterComponent]
public sealed partial class FastAndFuriousComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public float SprintModifier = 1.6f;

    [ViewVariables(VVAccess.ReadWrite)]
    public float WalkModifier = 1;
}
