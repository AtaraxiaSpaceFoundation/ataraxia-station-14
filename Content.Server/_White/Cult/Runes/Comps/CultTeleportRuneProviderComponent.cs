namespace Content.Server._White.Cult.Runes.Comps;

[RegisterComponent]
public sealed partial class CultTeleportRuneProviderComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? BaseRune;

    [ViewVariables(VVAccess.ReadOnly)]
    public List<EntityUid>? Targets;
}
