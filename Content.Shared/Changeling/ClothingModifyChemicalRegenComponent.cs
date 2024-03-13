namespace Content.Shared.Changeling;

[RegisterComponent]
public sealed partial class ClothingModifyChemicalRegenComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Multiplier = 0.75f;
}
