using Robust.Shared.Prototypes;

namespace Content.Shared._White.MagGloves;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent, AutoGenerateComponentState]
public sealed partial class MagneticGlovesComponent : Component
{
    [ViewVariables]
    public bool Enabled { get; set; } = false;

    [DataField, AutoNetworkedField]
    public EntityUid? ToggleActionEntity;

    [DataField("action")]
    public EntProtoId ToggleAction = "ActionToggleMagneticGloves";

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan GlovesReadyAt = TimeSpan.Zero;

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan GlovesLastActivation = TimeSpan.Zero;

    [DataField("glovesCooldown")]
    public TimeSpan GlovesCooldown = TimeSpan.FromSeconds(60);

    [DataField("glovesActiveTime")]
    public TimeSpan GlovesActiveTime = TimeSpan.FromSeconds(60);
}
