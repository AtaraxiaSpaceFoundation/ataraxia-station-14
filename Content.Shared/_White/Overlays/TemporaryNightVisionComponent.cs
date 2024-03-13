using Robust.Shared.GameStates;

namespace Content.Shared._White.Overlays;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TemporaryNightVisionComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public Vector3 Tint = new(0.3f, 0.3f, 0.3f);

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float Strength = 2f;

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float Noise = 0.5f;

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public Color Color = Color.FromHex("#FB9898");
}
