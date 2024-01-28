namespace Content.Shared._White.Cult.Components;

[RegisterComponent]
public sealed partial class CultBuffComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly), DataField("buffTime")]
    public TimeSpan BuffTime = TimeSpan.FromSeconds(60);

    public static float NearbyTilesBuffRadius = 1f;

    public static readonly TimeSpan CultTileBuffTime = TimeSpan.FromSeconds(5);
}
