using Robust.Shared.Serialization;

namespace Content.Shared.White.TTS;

[Serializable, NetSerializable]
// ReSharper disable once InconsistentNaming
public sealed class PlayTTSEvent : EntityEventArgs
{
    public NetEntity Uid { get; }
    public byte[] Data { get; }

    public PlayTTSEvent(NetEntity uid, byte[] data)
    {
        Uid = uid;
        Data = data;
    }
}
