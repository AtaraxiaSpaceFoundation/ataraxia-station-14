namespace Content.Shared._White.Keyhole.Components;

[RegisterComponent]
public abstract partial class KeyBaseComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public int? FormId;
}