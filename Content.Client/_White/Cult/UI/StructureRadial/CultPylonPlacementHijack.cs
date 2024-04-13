using System.Linq;
using Content.Client.Construction;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Popups;
using Robust.Client.Placement;
using Robust.Client.Utility;
using Robust.Shared.Map;

namespace Content.Client._White.Cult.UI.StructureRadial;

public sealed class CultPylonPlacementHijack : PlacementHijack
{
    private readonly ConstructionSystem _constructionSystem;
    private readonly IEntityManager _entMan;
    private readonly ConstructionPrototype? _prototype;
    private readonly EntityUid _player;

    public override bool CanRotate { get; }

    public CultPylonPlacementHijack(ConstructionPrototype? prototype, IEntityManager entMan, EntityUid player)
    {
        _prototype = prototype;
        _entMan = entMan;
        _player = player;
        _constructionSystem = entMan.System<ConstructionSystem>();
        CanRotate = prototype?.CanRotate ?? true;
    }

    /// <inheritdoc />
    public override bool HijackPlacementRequest(EntityCoordinates coordinates)
    {
        if (_prototype == null)
            return true;

        if (CheckForStructure(coordinates))
        {
            var popup = _entMan.System<SharedPopupSystem>();
            popup.PopupClient(Loc.GetString("cult-structure-craft-another-structure-nearby"), _player, _player);
            return true;
        }

        _constructionSystem.ClearAllGhosts();
        var dir = Manager.Direction;
        _constructionSystem.SpawnGhost(_prototype, coordinates, dir);

        return true;
    }

    private bool CheckForStructure(EntityCoordinates coordinates)
    {
        var lookupSystem = _entMan.System<EntityLookupSystem>();
        var entities = lookupSystem.GetEntitiesInRange(coordinates, 10f);
        foreach (var ent in entities)
        {
            if (!_entMan.TryGetComponent<MetaDataComponent>(ent, out var metadata))
                continue;

            if (metadata.EntityPrototype?.ID is "CultPylon")
                return true;
        }

        return false;
    }

    /// <inheritdoc />
    public override bool HijackDeletion(EntityUid entity)
    {
        if (IoCManager.Resolve<IEntityManager>().HasComponent<ConstructionGhostComponent>(entity))
        {
            _constructionSystem.ClearGhost(entity.GetHashCode());
        }

        return true;
    }

    /// <inheritdoc />
    public override void StartHijack(PlacementManager manager)
    {
        base.StartHijack(manager);
        manager.CurrentTextures = _prototype?.Layers.Select(sprite => sprite.DirFrame0()).ToList();
    }
}
