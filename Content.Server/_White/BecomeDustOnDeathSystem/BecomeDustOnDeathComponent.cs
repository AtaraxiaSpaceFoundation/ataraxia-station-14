using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;

namespace Content.Server._White.BecomeDustOnDeathSystem;

[RegisterComponent]
public sealed partial class BecomeDustOnDeathComponent : Component
{
    [DataField("sprite", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string SpawnOnDeathPrototype = "Ectoplasm";
}
