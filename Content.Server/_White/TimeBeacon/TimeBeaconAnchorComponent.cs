namespace Content.Server._White.TimeBeacon;

[RegisterComponent]
public sealed partial class TimeBeaconAnchorComponent : Component
{
    [ViewVariables]
    public EntityUid Entity = EntityUid.Invalid;

    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(10);
}
