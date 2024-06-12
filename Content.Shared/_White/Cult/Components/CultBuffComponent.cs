namespace Content.Shared._White.Cult.Components;

[RegisterComponent]
public sealed partial class CultBuffComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly), DataField]
    public TimeSpan BuffTime = TimeSpan.FromSeconds(60);

    [ViewVariables(VVAccess.ReadOnly), DataField]
    public TimeSpan StartingBuffTime = TimeSpan.FromSeconds(60);

    [ViewVariables(VVAccess.ReadOnly), DataField]
    public TimeSpan BuffLimit = TimeSpan.FromSeconds(55);

    public static float NearbyTilesBuffRadius = 1f;

    public static readonly TimeSpan CultTileBuffTime = TimeSpan.FromSeconds(1);
}
