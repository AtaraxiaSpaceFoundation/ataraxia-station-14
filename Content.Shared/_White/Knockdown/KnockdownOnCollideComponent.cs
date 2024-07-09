namespace Content.Shared._White.Knockdown;

[RegisterComponent]
public sealed partial class KnockdownOnCollideComponent : Component
{
    [DataField]
    public float BlurTime = 20f;

    [DataField]
    public bool UseBlur;
}
