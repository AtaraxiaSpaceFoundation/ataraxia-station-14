using Robust.Shared.GameStates;

namespace Content.Shared._White.Carrying;

[RegisterComponent, NetworkedComponent, Access(typeof(CarryingSlowdownSystem)), AutoGenerateComponentState]
public sealed partial class CarryingSlowdownComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float WalkModifier = 0.7f;

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float SprintModifier = 0.7f;
}