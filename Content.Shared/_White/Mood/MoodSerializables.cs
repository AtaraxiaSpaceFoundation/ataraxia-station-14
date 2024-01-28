using Robust.Shared.Serialization;

namespace Content.Shared._White.Mood;

[Serializable, NetSerializable]
public enum MoodChangeLevel : byte
{
    None,
    Small,
    Medium,
    Big,
    Huge,
    Large
}

[Serializable, NetSerializable]
public sealed partial class MoodEffectEvent : EntityEventArgs
{
    public string EffectId;

    public MoodEffectEvent(string effectId)
    {
        EffectId = effectId;
    }
}

[Serializable, NetSerializable]
public sealed partial class MoodRemoveEffectEvent : EntityEventArgs
{
    public string EffectId;

    public MoodRemoveEffectEvent(string effectId)
    {
        EffectId = effectId;
    }
}
