namespace Content.Server.Revenant.Components;

[RegisterComponent]
public sealed partial class BlightComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan MaxDuration;

    [ViewVariables(VVAccess.ReadWrite)]
    public float Duration;

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan MaxDelay;

    [ViewVariables(VVAccess.ReadWrite)]
    public float Delay;

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan SleepingCureTime = TimeSpan.FromSeconds(25);

    [ViewVariables(VVAccess.ReadWrite)]
    public float SleepDelay;

    [ViewVariables]
    public bool BedSleep;
}
