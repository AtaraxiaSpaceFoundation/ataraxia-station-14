using Robust.Shared.Prototypes;

namespace Content.Shared._White.Chaplain;

[RegisterComponent]
public sealed partial class ArmamentsBeaconComponent : Component
{
    [DataField]
    public List<EntProtoId> Armor = new();

    [DataField]
    public List<EntProtoId?> Helmets = new();
}
