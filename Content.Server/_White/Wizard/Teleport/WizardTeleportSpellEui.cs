using Content.Server.EUI;
using Content.Server.Popups;
using Content.Shared._White.Wizard.Teleport;
using Content.Shared.Eui;
using Robust.Shared.Timing;

namespace Content.Server._White.Wizard.Teleport;

public sealed class WizardTeleportSpellEui : BaseEui
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    private readonly SharedTransformSystem _transformSystem;
    private readonly TeleportLocationSystem _teleportLocation;
    private readonly PopupSystem _popupSystem;

    private readonly EntityUid _performer;

    private bool _used;

    public WizardTeleportSpellEui(EntityUid performer)
    {
        IoCManager.InjectDependencies(this);

        _transformSystem = _entityManager.System<SharedTransformSystem>();
        _teleportLocation = _entityManager.System<TeleportLocationSystem>();
        _popupSystem = _entityManager.System<PopupSystem>();

        _performer = performer;

        Timer.Spawn(TimeSpan.FromSeconds(60), Close);
    }

    public override EuiStateBase GetNewState()
    {
        var locationQuery = _entityManager.EntityQueryEnumerator<TeleportLocationComponent, TransformComponent>();
        var state = new WizardTeleportSpellEuiState();

        while (locationQuery.MoveNext(out var locationUid, out var locationComponent, out var transformComponent))
        {
            if (_teleportLocation.CanTeleport(locationUid, transformComponent))
                state.Locations.Add((int) locationUid, locationComponent.Location);
        }

        return state;
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (_used)
        {
            return;
        }

        if (msg is not TeleportSpellTargetLocationSelected cast)
        {
            return;
        }

        var transform = _entityManager.GetComponent<TransformComponent>(_performer);
        var oldCoords = transform.Coordinates;

        TransformComponent? locationTransform = null;

        var teleportLocationQuery = _entityManager
            .EntityQueryEnumerator<TeleportLocationComponent, TransformComponent>();
        while (teleportLocationQuery.MoveNext(out var locationUid, out _, out var transformComponent))
        {
            if (locationUid == new EntityUid(cast.LocationUid))
            {
                locationTransform = transformComponent;
            }
        }

        if (locationTransform is null)
        {
            _popupSystem.PopupEntity("Can't teleport", _performer, _performer);
            DoStateUpdate();
            return;
        }

        _used = true;

        var coords = locationTransform.Coordinates;

        _transformSystem.SetCoordinates(_performer, coords);
        _transformSystem.AttachToGridOrMap(_performer, transform);

        _entityManager.SpawnEntity("AdminInstantEffectSmoke10", oldCoords);
        _entityManager.SpawnEntity("AdminInstantEffectSmoke10", coords);

        Close();
    }
}
