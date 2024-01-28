using Content.Shared._White.Supermatter.Components;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Supermatter.Systems;

public abstract class SharedSupermatterSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SupermatterComponent, ComponentStartup>(OnSupermatterStartup);
    }

    public enum SuperMatterSound : sbyte
    {
        Aggressive = 0,
        Delam = 1
    }

    public enum DelamType : sbyte
    {
        Explosion = 0,
        Singulo = 1,
    }

    public void OnSupermatterStartup(EntityUid uid, SupermatterComponent comp, ComponentStartup args)
    {
    }

    /// <summary>
    /// A state wrapper used to sync the supermatter between the server and client.
    /// </summary>
    [Serializable, NetSerializable]
    protected sealed class SupermatterComponentState : ComponentState
    {
        public SupermatterComponentState(SupermatterComponent supermatter)
        {
        }
    }
}
