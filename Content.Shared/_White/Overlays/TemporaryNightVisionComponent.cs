using Robust.Shared.GameStates;

namespace Content.Shared._White.Overlays;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TemporaryNightVisionComponent : BaseNvOverlayComponent
{
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public override Color Color { get; set; } = Color.FromHex("#FB9898");
}
