namespace Content.Server._White.Cult.Runes.Comps;

[RegisterComponent]
public sealed partial class CultRuneOfferingComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("sacrificeDeadMinCount")]
    public uint SacrificeDeadMinCount = 1;

    [ViewVariables(VVAccess.ReadWrite), DataField("convertMinCount")]
    public uint ConvertMinCount = 2;

    [ViewVariables(VVAccess.ReadWrite), DataField("sacrificeMinCount")]
    public uint SacrificeMinCount = 3;

    [ViewVariables(VVAccess.ReadWrite), DataField("rangeTarget")]
    public float RangeTarget = 0.3f;
}
