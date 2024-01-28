using Content.Server.UserInterface;
using Content.Shared._White.Cult;
using Content.Shared._White.Cult.UI;
using Robust.Server.GameObjects;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server._White.Cult.TimedProduction;

[RegisterComponent]
public sealed partial class CultistFactoryComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    [DataField("cooldown")]
    public int Cooldown = 240;

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan? NextTimeUse;

    [ViewVariables(VVAccess.ReadOnly)]
    [DataField("products", customTypeSerializer: typeof(PrototypeIdListSerializer<CultistFactoryProductionPrototype>))]
    public IReadOnlyCollection<string> Products = ArraySegment<string>.Empty;

    public Enum UserInterfaceKey = CultistAltarUiKey.Key;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool Active = true;
}
