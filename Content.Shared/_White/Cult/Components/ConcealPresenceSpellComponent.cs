using Content.Shared.Actions;
using Robust.Shared.Utility;

namespace Content.Shared._White.Cult.Components;

[RegisterComponent]
public sealed partial class ConcealPresenceSpellComponent : Component
{
    [ViewVariables]
    public bool Revealing;

    [DataField(required: true), NonSerialized]
    public InstantActionEvent? ConcealEvent;

    [DataField(required: true), NonSerialized]
    public InstantActionEvent? RevealEvent;

    [DataField]
    public SpriteSpecifier? ConcealIcon;

    [DataField]
    public SpriteSpecifier? RevealIcon;
}
