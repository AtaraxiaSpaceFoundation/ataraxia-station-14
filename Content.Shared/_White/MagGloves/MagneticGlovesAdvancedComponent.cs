using Robust.Shared.Prototypes;

namespace Content.Shared._White.MagGloves;

/// <summary>
/// This is used as a marker for advanced magnetic gloves.
/// </summary>
[RegisterComponent, AutoGenerateComponentState]
public sealed partial class MagneticGlovesAdvancedComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? ToggleActionEntity;

    [DataField]
    public EntProtoId ToggleAction = "ActionToggleMagneticGlovesAdvanced";
}
