using Robust.Shared.GameStates;

namespace Content.Shared._White.Overlays;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NightVisionComponent : Component
{
    [DataField("tint"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public Vector3 Tint = new(0.3f, 0.3f, 0.3f);

    [DataField("strength"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float Strength = 2f;

    [DataField("noise"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float Noise = 0.5f;

    [DataField("color"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public Color Color = Color.FromHex("#98FB98");
}
