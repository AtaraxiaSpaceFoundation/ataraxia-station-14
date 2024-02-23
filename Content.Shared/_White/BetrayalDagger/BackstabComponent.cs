namespace Content.Shared._White.BetrayalDagger;

[RegisterComponent]
public sealed partial class BackstabComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float DamageMultiplier = 2f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool PenetrateArmor = true;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public Angle Tolerance = Angle.FromDegrees(45d);
}
