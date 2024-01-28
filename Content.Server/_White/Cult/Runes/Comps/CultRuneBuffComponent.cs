namespace Content.Server._White.Cult.Runes.Comps;

[RegisterComponent]
public sealed partial class CultRuneBuffComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("rangeTarget")]
    public float RangeTarget = 0.3f;
}
