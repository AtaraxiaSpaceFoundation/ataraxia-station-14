using Robust.Shared.GameStates;

namespace Content.Shared._White.Lighting.PointLight.Locker;

[RegisterComponent, NetworkedComponent]
public sealed partial class PointLightLockerComponent : Component
{
    [DataField, ViewVariables]
    public string RedColor = "#D56C6C";

    [DataField, ViewVariables]
    public string GreenColor = "#7FC080";

    [DataField, ViewVariables]
    public float ReduceEnergyOnOpen = 0.1f;

    [DataField, ViewVariables]
    public float ReduceRadiusOnOpen = 0.1f;
}
