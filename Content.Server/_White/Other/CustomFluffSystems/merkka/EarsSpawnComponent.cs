using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._White.Other.CustomFluffSystems.merkka;

[RegisterComponent]
public sealed partial class EarsSpawnComponent : Component
{
    [DataField("summonActionEars", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string SummonActionEars = "ActionEarsSummon";

    [DataField("summonActionCat", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string SummonActionCat = "ActionCatSummon";

    [DataField("summonActionEntityEars")] public EntityUid? SummonActionEntityEars;
    [DataField("summonActionEntityCat")] public EntityUid? SummonActionEntityCat;

    public int CatEarsUses = 30;
    public int СatSpawnUses = 20;
}
