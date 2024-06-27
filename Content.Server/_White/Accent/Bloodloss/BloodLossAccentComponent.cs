namespace Content.Server._White.Accent.Bloodloss;

[RegisterComponent]
public sealed partial class BloodLossAccentComponent : Component
{
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float ReplaceProb = 0.6f;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public string ToReplace = "...";
}
