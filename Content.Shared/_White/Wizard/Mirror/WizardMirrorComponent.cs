namespace Content.Shared._White.Wizard.Mirror;

[RegisterComponent]
public sealed partial class WizardMirrorComponent : Component
{
    [DataField]
    public EntityUid? Target;
}
