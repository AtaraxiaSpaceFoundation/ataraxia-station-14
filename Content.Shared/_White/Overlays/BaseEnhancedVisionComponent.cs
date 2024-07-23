using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Overlays;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public abstract partial class BaseEnhancedVisionComponent : BaseNvOverlayComponent
{
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool IsActive = true;

    [DataField]
    public virtual SoundSpecifier? ActivateSound { get; set; }= new SoundPathSpecifier("/Audio/White/Items/Goggles/activate.ogg");

    [DataField]
    public virtual SoundSpecifier? DeactivateSound { get; set; } = new SoundPathSpecifier("/Audio/White/Items/Goggles/deactivate.ogg");

    [DataField]
    public virtual EntProtoId? ToggleAction { get; set; }

    [ViewVariables]
    public EntityUid? ToggleActionEntity;
}
