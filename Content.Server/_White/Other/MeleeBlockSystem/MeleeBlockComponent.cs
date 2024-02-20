namespace Content.Server._White.Other.MeleeBlockSystem;

[RegisterComponent]
public sealed partial class MeleeBlockComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float BlockChance = 0.4f;
}
