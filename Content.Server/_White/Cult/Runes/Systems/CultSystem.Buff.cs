using System.Linq;
using System.Numerics;
using Content.Server._White.Cult.GameRule;
using Content.Shared.Alert;
using Content.Shared.Maps;
using Content.Shared._White.Cult.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using CultistComponent = Content.Shared._White.Cult.Components.CultistComponent;

namespace Content.Server._White.Cult.Runes.Systems;

public partial class CultSystem
{
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefinition = default!;
    [Dependency] private readonly MapSystem _map = default!;

    public void InitializeBuffSystem()
    {
        SubscribeLocalEvent<CultBuffComponent, ComponentAdd>(OnAdd);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        UpdateBuffTimers(frameTime);
        AnyCultistNearTile();
        RemoveExpiredBuffs();
    }

    private void AnyCultistNearTile()
    {
        var cultistsQuery = EntityQueryEnumerator<CultistComponent>();

        while (cultistsQuery.MoveNext(out var uid, out _))
        {
            if (HasComp<CultBuffComponent>(uid))
                continue;

            if (!AnyCultTilesNearby(uid))
                continue;

            var comp = EnsureComp<CultBuffComponent>(uid);
            comp.BuffTime = CultBuffComponent.CultTileBuffTime;
        }
    }

    private void OnAdd(EntityUid uid, CultBuffComponent comp, ComponentAdd args)
    {
        _alertsSystem.ShowAlert(uid, AlertType.CultBuffed);
    }

    private void UpdateBuffTimers(float frameTime)
    {
        var buffsQuery = EntityQueryEnumerator<CultBuffComponent>();

        while (buffsQuery.MoveNext(out var uid, out var buff))
        {
            var remainingTime = buff.BuffTime;

            remainingTime -= TimeSpan.FromSeconds(frameTime);

            if (HasComp<CultistComponent>(uid))
            {
                if (remainingTime < CultBuffComponent.CultTileBuffTime && AnyCultTilesNearby(uid))
                    remainingTime = CultBuffComponent.CultTileBuffTime;
            }

            buff.BuffTime = remainingTime;
        }
    }

    private bool AnyCultTilesNearby(EntityUid uid)
    {
        var localpos = Transform(uid).Coordinates.Position;

        if (!TryComp<CultistComponent>(uid, out _))
            return false;

        var radius = CultBuffComponent.NearbyTilesBuffRadius;

        var gridUid = Transform(uid).GridUid;
        if (!gridUid.HasValue)
        {
            return false;
        }

        if (!TryComp(gridUid, out MapGridComponent? grid))
            return false;

        var tilesRefs = _map.GetLocalTilesIntersecting(gridUid.Value, grid, new Box2(
            localpos + new Vector2(-radius, -radius),
            localpos + new Vector2(radius, radius)));

        var cultRule = EntityManager.EntityQuery<CultRuleComponent>().FirstOrDefault();
        if (cultRule is null)
        {
            return false;
        }

        var cultTileDef = (ContentTileDefinition) _tileDefinition[$"{cultRule.CultFloor}"];
        var cultTile = new Tile(cultTileDef.TileId);

        return tilesRefs.Any(tileRef => tileRef.Tile.TypeId == cultTile.TypeId);
    }

    private void RemoveExpiredBuffs()
    {
        var buffsQuery = EntityQueryEnumerator<CultBuffComponent>();

        while (buffsQuery.MoveNext(out var uid, out var buff))
        {
            var remainingTime = buff.BuffTime;

            if (remainingTime <= TimeSpan.Zero)
            {
                RemComp<CultBuffComponent>(uid);
                _alertsSystem.ClearAlert(uid, AlertType.CultBuffed);
            }
        }
    }
}