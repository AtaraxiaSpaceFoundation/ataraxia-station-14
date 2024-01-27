﻿namespace Content.Client._White.Cult.Items.VeilShifter;

[RegisterComponent]
public sealed partial class VeilVisualsComponent : Component
{
    [DataField("stateOn")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string? StateOn = "icon-on";

    [DataField("stateOff")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string? StateOff = "icon";
}
