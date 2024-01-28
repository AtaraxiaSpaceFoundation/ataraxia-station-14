namespace Content.Server._White.Cult.Runes.Comps;

[RegisterComponent]
public sealed partial class CultRuneSummoningProviderComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? BaseRune;
}
