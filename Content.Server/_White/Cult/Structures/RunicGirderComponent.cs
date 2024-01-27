namespace Content.Server.White.Cult.Structures;

[RegisterComponent]
public sealed partial class RunicGirderComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public string UsedItemID = "RitualDagger";

    [ViewVariables(VVAccess.ReadOnly)]
    public string DropItemID = "CultRunicMetal1";
}
