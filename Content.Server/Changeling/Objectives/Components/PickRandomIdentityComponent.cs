namespace Content.Server.Changeling.Objectives.Components;

[RegisterComponent]
public sealed partial class PickRandomIdentityComponent : Component
{
    [ViewVariables]
    public string DNA = string.Empty;
}
