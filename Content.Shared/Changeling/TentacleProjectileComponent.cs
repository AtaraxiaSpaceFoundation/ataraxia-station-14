using Robust.Shared.GameStates;

namespace Content.Shared.Changeling;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TentacleProjectileComponent : Component
{
    /// <summary>
    ///     Time until projectile despawns in miliseconds.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int DespawnTime = -1;

    [ViewVariables, AutoNetworkedField]
    public EntityUid? GrabbedUid;
}
