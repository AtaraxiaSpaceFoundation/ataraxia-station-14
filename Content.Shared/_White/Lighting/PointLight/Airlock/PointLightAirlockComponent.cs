using Robust.Shared.GameStates;

namespace Content.Shared._White.Lighting.PointLight.Airlock;

[RegisterComponent, NetworkedComponent]
public sealed partial class PointLightAirlockComponent : Component
{
    [ViewVariables]
    public string RedColor = "#D56C6C";

    [ViewVariables]
    public string BlueColor = "#7F93C0";

    [ViewVariables]
    public string YellowColor = "#BDC07F";

    [ViewVariables]
    public string GreenColor = "#7FC080";
}
