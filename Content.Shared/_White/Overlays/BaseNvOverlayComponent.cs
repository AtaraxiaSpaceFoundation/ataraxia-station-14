using Robust.Shared.GameStates;

namespace Content.Shared._White.Overlays;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public abstract partial class BaseNvOverlayComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public virtual Vector3 Tint { get; set; } = new(0.3f, 0.3f, 0.3f);

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public virtual float Strength { get; set; } = 2f;

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public virtual float Noise { get; set; } = 0.5f;

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public virtual Color Color { get; set; } = Color.FromHex("#98FB98");
}
