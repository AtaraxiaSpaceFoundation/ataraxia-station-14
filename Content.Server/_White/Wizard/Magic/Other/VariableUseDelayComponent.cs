namespace Content.Server._White.Wizard.Magic.Other;

[RegisterComponent]
public sealed partial class VariableUseDelayComponent : Component
{
    [DataField]
    public TimeSpan UseDelay = TimeSpan.FromSeconds(6);

    [DataField]
    public TimeSpan AltUseDelay = TimeSpan.FromSeconds(12);

    [DataField]
    public TimeSpan ChargeUseDelay = TimeSpan.FromSeconds(40);
}
