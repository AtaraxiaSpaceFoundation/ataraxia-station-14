using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._White.CPR.Events;

[Serializable, NetSerializable]
public sealed partial class CPREndedEvent : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
}
