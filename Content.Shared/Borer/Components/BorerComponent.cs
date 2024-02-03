using Robust.Shared.GameStates;

namespace Content.Shared.Borer;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class BorerComponent : Component
{
    public string ActionInfest = "ActionInfest";

    public EntityUid? ActionInfestEntity;

    public string ActionStun = "ActionBorerStunVictim";

    public EntityUid? ActionStunEntity;

    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public int Points = 0;
}
