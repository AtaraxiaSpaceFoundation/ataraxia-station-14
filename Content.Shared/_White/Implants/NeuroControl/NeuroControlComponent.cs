using Content.Shared.Tag;
using Robust.Shared.GameStates;

namespace Content.Shared._White.Implants.NeuroControl;

[RegisterComponent, NetworkedComponent]
public sealed partial class NeuroControlComponent : Component
{
    [ValidatePrototypeId<TagPrototype>]
    public const string NeuroControlTag = "NeuroControl";
}
