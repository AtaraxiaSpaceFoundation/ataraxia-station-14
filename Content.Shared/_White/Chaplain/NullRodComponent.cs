using Robust.Shared.Prototypes;

namespace Content.Shared._White.Chaplain;

[RegisterComponent]
public sealed partial class NullRodComponent : Component
{
    [DataField]
    public List<EntProtoId> Weapons = new();
}
