namespace Content.Server._White.Other.CritSystem;

[RegisterComponent]
public sealed partial class CritComponent : Component
{
    [DataField("critChance", required: true)]
    [ViewVariables(VVAccess.ReadWrite)]
    public int CritChance = 25;

    [DataField("critMultiplier", required: true)]
    [ViewVariables(VVAccess.ReadWrite)]
    public float CritMultiplier = 2.5f;

    [DataField("isBloodDagger")]
    public bool IsBloodDagger;

    [DataField("workingChance")]
    public int? WorkingChance;
}
