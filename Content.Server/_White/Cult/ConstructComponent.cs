using Robust.Shared.Prototypes;

namespace Content.Server._White.Cult;

[RegisterComponent]
public sealed partial class ConstructComponent : Component
{
    [DataField("actions")]
    public List<EntProtoId> Actions = new();

    [ViewVariables]
    public List<EntityUid?> ActionEntities = new();
}
