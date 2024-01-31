namespace Content.Server._White.Other.ChangeThrowForceSystem;

[RegisterComponent]
public sealed partial class ChangeThrowForceComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ThrowForce = 10f;
}
