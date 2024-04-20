using Robust.Shared.Serialization;

namespace Content.Shared.MedicalScanner;

/// <summary>
///     On interacting with an entity retrieves the entity UID for use with getting the current damage of the mob.
/// </summary>
[Serializable, NetSerializable]
public sealed class HealthAnalyzerScannedUserMessage(
    NetEntity? targetEntity,
    float temperature,
    float bloodLevel,
    bool? scanMode,
    bool? bleeding)
    : BoundUserInterfaceMessage
{
    public readonly NetEntity? TargetEntity = targetEntity;
    public float Temperature = temperature;
    public float BloodLevel = bloodLevel;
    public bool? ScanMode = scanMode;
    public bool? Bleeding = bleeding;
}

