using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._White.Other;

[RegisterComponent]
public sealed partial class WindowMarkComponent : Component
{
    [DataField("replacementProto", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ReplacementProto = default!;
}
