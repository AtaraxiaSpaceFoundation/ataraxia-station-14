using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.ClothingGrant.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ClothingGrantComponentComponent : Component
{
    [DataField("component", required: true)]
    [AlwaysPushInheritance]
    public ComponentRegistry Components { get; private set; } = new();

    [ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public bool IsActive = false;
}
