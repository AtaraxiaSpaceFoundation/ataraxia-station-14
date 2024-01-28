using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared._White.Cult.Components;

[RegisterComponent]
public sealed partial class ConstructShellComponent : Component
{
    [DataField("shardSlot", required: true)]
    public ItemSlot ShardSlot = new();

    public readonly string ShardSlotId = "Shard";

    [DataField("constructForms", customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>))]
    public List<string> ConstructForms = new();
}
