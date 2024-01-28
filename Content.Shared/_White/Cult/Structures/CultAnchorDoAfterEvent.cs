using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Cult.Structures;

[Serializable, NetSerializable]
public sealed partial class CultAnchorDoAfterEvent : SimpleDoAfterEvent
{
    public bool IsAnchored;

    public CultAnchorDoAfterEvent(bool isAnchored)
    {
        IsAnchored = isAnchored;
    }
}
