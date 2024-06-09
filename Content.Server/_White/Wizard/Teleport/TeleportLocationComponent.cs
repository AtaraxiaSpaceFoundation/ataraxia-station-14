namespace Content.Server._White.Wizard.Teleport;

[RegisterComponent]
public sealed partial class TeleportLocationComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public string Location = string.Empty;
}
