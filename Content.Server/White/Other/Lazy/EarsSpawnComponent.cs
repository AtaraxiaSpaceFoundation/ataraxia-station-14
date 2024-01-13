using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.White.Other.Lazy;

[RegisterComponent]
public sealed partial class EarsSpawnComponent : Component
{
    [DataField("summonAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string SummonAction = "ActionEarsSummon";

    [DataField("summonActionEntity")] public EntityUid? SummonActionEntity;
}
