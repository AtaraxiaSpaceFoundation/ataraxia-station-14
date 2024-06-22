namespace Content.Server._White.Wizard;


[RegisterComponent]
public sealed partial class WizardComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public bool EndRoundOnDeath;
}
