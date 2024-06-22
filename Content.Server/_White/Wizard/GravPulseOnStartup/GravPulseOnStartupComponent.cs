namespace Content.Server._White.Wizard.GravPulseOnStartup;

[RegisterComponent]
public sealed partial class GravPulseOnStartupComponent : Component
{
    [DataField]
    public float MaxRange;

    [DataField]
    public float MinRange;

    [DataField]
    public float BaseRadialAcceleration;

    [DataField]
    public float BaseTangentialAcceleration;

    [DataField]
    public float StunTime;
}
