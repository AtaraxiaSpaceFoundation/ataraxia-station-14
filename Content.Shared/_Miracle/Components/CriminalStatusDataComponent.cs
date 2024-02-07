using Content.Shared.Security;
using Robust.Shared.GameStates;

namespace Content.Shared._Miracle.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CriminalStatusDataComponent : Component
{
    [AutoNetworkedField, ViewVariables]
    public Dictionary<string, SecurityStatus> Statuses = new();
}
