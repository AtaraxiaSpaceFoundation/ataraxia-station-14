using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Keyhole;

[Serializable, NetSerializable]
public sealed partial class KeyInsertDoAfterEvent : SimpleDoAfterEvent
{
    public int FormId;

    public KeyInsertDoAfterEvent(int formId)
    {
        FormId = formId;
    }
}

