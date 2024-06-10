using Content.Server.Pinpointer;
using Content.Server.Station.Systems;
using Content.Server.Warps;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Maps;
using Content.Shared.Physics;

namespace Content.Server._White.Wizard.Teleport;

public sealed class TeleportLocationSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

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

        var xForm = Transform(ent);

        if (!CanTeleport(ent, xForm))
            return;

        var newEnt = Spawn(null, xForm.Coordinates);
        var newXForm = EnsureComp<TransformComponent>(newEnt);
        _transformSystem.AttachToGridOrMap(newEnt, newXForm);
        var location = EnsureComp<TeleportLocationComponent>(newEnt);
        location.Location = warpPoint.Location;
    }

    public bool CanTeleport(EntityUid uid, TransformComponent xForm)
    {
        var station = _station.GetOwningStation(uid, xForm);

        if (!HasComp<TeleportLocationTargetStationComponent>(station))
            return false;

        var turf = xForm.Coordinates.SnapToGrid(EntityManager).GetTileRef(EntityManager);

        if (turf == null)
            return false;

        return !_turf.IsTileBlocked(turf.Value, CollisionGroup.Impassable);
    }
}
