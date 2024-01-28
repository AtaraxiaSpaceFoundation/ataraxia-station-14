using Content.Shared.Actions;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Administration;

[RegisterComponent]
[Access(typeof(SharedInvisibilitySystem))]
public sealed partial class InvisibilityComponent : Component
{
    [ViewVariables]
    public bool Invisible;

    public float? DefaultAlpha;

    [DataField]
    public EntProtoId ToggleInvisibilityAction = "ToggleInvisibilityAction";

    [DataField]
    public EntityUid? ToggleInvisibilityActionEntity;
}

public sealed partial class ToggleInvisibilityActionEvent : InstantActionEvent
{
}
