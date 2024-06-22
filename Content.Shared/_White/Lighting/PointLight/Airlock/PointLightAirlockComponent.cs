using Robust.Shared.GameStates;

namespace Content.Shared._White.Lighting.PointLight.Airlock;

[RegisterComponent, NetworkedComponent]
public sealed partial class PointLightAirlockComponent : Component
{
    [ViewVariables]
    public bool IsLightsEnabled;

    [ViewVariables]
    public bool LastBoltsState;

    [ViewVariables]
    public readonly string RedColor = "#D56C6C";

    [ViewVariables]
    public readonly string BlueColor = "#7F93C0";

    [ViewVariables]
    public readonly string YellowColor = "#BDC07F";

    [ViewVariables]
    public readonly string GreenColor = "#7FC080";
}
