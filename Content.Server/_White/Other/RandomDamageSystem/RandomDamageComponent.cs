namespace Content.Server._White.Other.RandomDamageSystem;

[RegisterComponent]
public sealed partial class RandomDamageComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Max = 50f;
}
