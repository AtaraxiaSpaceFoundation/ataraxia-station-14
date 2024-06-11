namespace Content.Server._White.Wizard.SpellBlade;

[RegisterComponent]
public sealed partial class LightningAspectComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Range = 2f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int BoltCount = 3;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string LightningPrototype = "WeakWizardLightning";

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int ArcDepth = 2;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan ShockRate = TimeSpan.FromSeconds(10);

    public TimeSpan NextShock;
}
