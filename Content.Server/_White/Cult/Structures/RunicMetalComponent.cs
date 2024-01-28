using Content.Server.UserInterface;
using Content.Shared.White.Cult.Structures;
using Robust.Server.GameObjects;

namespace Content.Server._White.Cult.Structures;

[RegisterComponent]
public sealed partial class RunicMetalComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public Enum UserInterfaceKey = CultStructureCraftUiKey.Key;

    [ViewVariables(VVAccess.ReadWrite), DataField("delay")]
    public float Delay = 1;
}
