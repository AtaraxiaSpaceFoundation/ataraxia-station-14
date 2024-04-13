using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._White.TimeBeacon;

[RegisterComponent]
public sealed partial class TimeBeaconComponent : Component
{
    [ViewVariables]
    public TimeSpan NextUse = TimeSpan.Zero;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(20);

    [DataField]
    public EntProtoId AnchorEntity = "TimeBeaconAnchor";

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Machines/high_tech_confirm.ogg");
}
