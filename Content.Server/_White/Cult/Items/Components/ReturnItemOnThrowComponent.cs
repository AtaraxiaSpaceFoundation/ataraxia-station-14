namespace Content.Server.White.Cult.Items.Components;

[RegisterComponent]
public sealed partial class ReturnItemOnThrowComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("stunTime")]
    public float StunTime = 1f;
}
