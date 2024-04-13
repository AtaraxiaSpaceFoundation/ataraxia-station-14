using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Changeling;

[RegisterComponent]
public sealed partial class HiveHeadComponent : Component
{
    [DataField]
    public int BeesAmount = 4;

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string BeeProto = "MobTemporaryAngryBee";

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Action = "ActionReleaseBees";

    [DataField]
    public EntityUid? ActionEntity;
}
