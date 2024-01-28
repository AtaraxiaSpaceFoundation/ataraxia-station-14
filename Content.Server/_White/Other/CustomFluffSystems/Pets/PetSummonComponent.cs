using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._White.Other.CustomFluffSystems.Pets;

[RegisterComponent]
public sealed partial class PetSummonComponent : Component
{
    [DataField("petSummonAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string PetSummonAction = "PetSummonAction";

    [DataField("petGhostSummonAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string PetGhostSummonAction = "PetGhostSummonAction";

    [DataField("petSummonActionEntity")] public EntityUid? PetSummonActionEntity;
    [DataField("petGhostSummonActionEntity")] public EntityUid? PetGhostSummonActionEntity;

    public int UsesLeft = 10;

    public EntityUid? SummonedEntity;
}
