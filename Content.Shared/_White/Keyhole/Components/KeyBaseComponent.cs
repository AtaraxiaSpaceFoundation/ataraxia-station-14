namespace Content.Shared._White.Keyhole.Components;

[RegisterComponent, Virtual]
public partial class KeyBaseComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public int? FormId;
}
