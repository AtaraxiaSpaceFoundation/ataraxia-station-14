namespace Content.Client._White.Cult.Pylon;

[RegisterComponent]
public sealed partial class PylonVisualsComponent : Component
{
    [DataField("stateOn")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string? StateOn = "pylon";

    [DataField("stateOff")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string? StateOff = "pylon_off";
}
