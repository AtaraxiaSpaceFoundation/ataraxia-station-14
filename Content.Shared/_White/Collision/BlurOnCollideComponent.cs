namespace Content.Shared._White.Collision;

[RegisterComponent]
public sealed partial class BlurOnCollideComponent : Component
{
    [DataField]
    public float BlurTime = 20f;
}
