namespace Content.Shared.White.CheapSurgery;

[RegisterComponent]
public sealed partial class ActiveSurgeryComponent : Component
{
    [ViewVariables] public EntityUid OrganUid = EntityUid.Invalid;
}
