using Content.Server.EUI;
using Content.Server.Popups;
using Content.Server._White.Cult.Runes.Comps;
using Content.Shared.Eui;
using Content.Shared.Popups;
using Content.Shared._White.Cult.UI;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Robust.Shared.Timing;

namespace Content.Server._White.Cult.UI;

public sealed class CultTeleportSpellEui : BaseEui
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    private readonly SharedTransformSystem _transformSystem;
    private readonly PullingSystem _pulling;
    private readonly PopupSystem _popupSystem;

    private readonly EntityUid _performer;
    private readonly EntityUid _target;

    private bool _used;

    public CultTeleportSpellEui(EntityUid performer, EntityUid target)
    {
        IoCManager.InjectDependencies(this);

        _transformSystem = _entityManager.System<SharedTransformSystem>();
        _pulling = _entityManager.System<PullingSystem>();
        _popupSystem = _entityManager.System<PopupSystem>();

        _performer = performer;
        _target = target;

        Timer.Spawn(TimeSpan.FromSeconds(10), Close);
    }

    public override EuiStateBase GetNewState()
    {
        var runesQuery = _entityManager.EntityQueryEnumerator<CultRuneTeleportComponent>();
        var state = new CultTeleportSpellEuiState();

        while (runesQuery.MoveNext(out var runeUid, out var rune))
        {
            state.Runes.Add((int) runeUid, rune.Label!);
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

        if (msg is not TeleportSpellTargetRuneSelected cast)
        {
            return;
        }

        var performerPosition = _entityManager.GetComponent<TransformComponent>(_performer).Coordinates;
        var targetPosition = _entityManager.GetComponent<TransformComponent>(_target).Coordinates;

        performerPosition.TryDistance(_entityManager, targetPosition, out var distance);

        if (distance > 1.5f)
        {
            _popupSystem.PopupEntity("Too far", _performer, PopupType.Medium);
            return;
        }

        TransformComponent? runeTransform = null;

        var teleportRuneQuery = _entityManager.EntityQueryEnumerator<CultRuneTeleportComponent, TransformComponent>();
        while (teleportRuneQuery.MoveNext(out var runeUid, out _, out var transformComponent))
        {
            if (runeUid == new EntityUid(cast.RuneUid))
            {
                runeTransform = transformComponent;
            }
        }

        if (runeTransform is null)
        {
            _popupSystem.PopupEntity("Rune is gone", _performer);
            DoStateUpdate();
            return;
        }

        _used = true;

        // break pulls before portal enter so we dont break shit
        if (_entityManager.TryGetComponent<PullableComponent>(_target, out var pullable) && pullable.BeingPulled)
        {
            _pulling.TryStopPull(_target, pullable);
        }

        if (_entityManager.TryGetComponent<PullerComponent>(_target, out var pulling)
            && pulling.Pulling != null
            && _entityManager.TryGetComponent<PullableComponent>(pulling.Pulling.Value, out var subjectPulling))
        {
            _pulling.TryStopPull(pulling.Pulling.Value, subjectPulling);
        }

        _transformSystem.SetCoordinates(_target, runeTransform.Coordinates);
        Close();
    }
}
