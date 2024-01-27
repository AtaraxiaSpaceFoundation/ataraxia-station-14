namespace Content.Server.White.Cult.Runes.Comps;

[RegisterComponent]
public sealed partial class CultRuneSummoningComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("summonMinCount")]
    public uint SummonMinCount = 2;
}
