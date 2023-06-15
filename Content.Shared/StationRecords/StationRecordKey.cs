using Robust.Shared.Serialization;

namespace Content.Shared.StationRecords;

// Station record keys. These should be stored somewhere,
// preferably within an ID card.
[Serializable, NetSerializable]
public readonly struct StationRecordKey : IEquatable<StationRecordKey>
{
    [DataField("id")]
    public readonly uint Id;

    [DataField("station")]
    public readonly NetEntity OriginStation;

    public static StationRecordKey Invalid = default;

    public StationRecordKey(uint id, NetEntity originStation)
    {
        Id = id;
        OriginStation = originStation;
    }

    public bool Equals(StationRecordKey other)
    {
        return Id == other.Id && OriginStation.Id == other.OriginStation.Id;
    }

    public override bool Equals(object? obj)
    {
        return obj is StationRecordKey other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, OriginStation);
    }

    public bool IsValid() => OriginStation.IsValid();
}
