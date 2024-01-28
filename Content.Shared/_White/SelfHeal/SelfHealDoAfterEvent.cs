using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._White.SelfHeal;

[Serializable, NetSerializable]
public sealed partial class SelfHealDoAfterEvent : SimpleDoAfterEvent
{
}
