namespace Content.Shared._White.Keyhole.Components;

[RegisterComponent, Virtual]
public partial class KeyBaseComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public int? FormId;
}