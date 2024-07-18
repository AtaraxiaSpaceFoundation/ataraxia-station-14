namespace Content.Server._White.Wizard;


[RegisterComponent]
public sealed partial class WizardComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public bool EndRoundOnDeath;

    [DataField]
    public int MinAge = 90;

    [DataField]
    public int MaxAge = 170;

    [DataField]
    public string Hair = "WizardHair";

    [DataField]
    public string FacialHair = "WizardFacialHair";

    [DataField]
    public string Color = "WizardHairColor";

    [DataField]
    public string Name = "WizardNames";
}
