namespace Content.Server._White.Cult.HolyWater;

[RegisterComponent]
public sealed partial class BibleWaterConvertComponent : Component
{
    [DataField("convertedId"), ViewVariables(VVAccess.ReadWrite)]
    public string ConvertedId = "Water";

    [DataField("ConvertedToId"), ViewVariables(VVAccess.ReadWrite)]
    public string ConvertedToId = "HolyWater";
}
