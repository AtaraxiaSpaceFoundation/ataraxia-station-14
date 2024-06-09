using Content.Server.Pinpointer;
using Content.Server.Warps;

namespace Content.Server._White.Wizard.Teleport;

public sealed class TeleportLocationSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpawnTeleportLocationComponent, MapInitEvent>(OnMapInit,
            after: new[] {typeof(NavMapSystem)});
    }

    private void OnMapInit(Entity<SpawnTeleportLocationComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp(ent, out WarpPointComponent? warpPoint) || warpPoint.Location == null)
            return;

        var newEnt = Spawn(null, Transform(ent).Coordinates);
        var xForm = EnsureComp<TransformComponent>(newEnt);
        _transformSystem.AttachToGridOrMap(newEnt, xForm);
        var location = EnsureComp<TeleportLocationComponent>(newEnt);
        location.Location = warpPoint.Location;
    }
}
