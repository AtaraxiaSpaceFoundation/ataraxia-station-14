using System.Numerics;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Standing;
using Content.Shared.Throwing;
using Content.Shared._White.MagGloves;
using Content.Shared.Standing.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Physics.Components;

namespace Content.Server.Standing;

public sealed class StandingStateSystem : SharedStandingStateSystem
{
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly ThrowingSystem _throwingSystem = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StandingStateComponent, DropHandItemsEvent>(FallOver);
    }

    private void FallOver(EntityUid uid, StandingStateComponent component, DropHandItemsEvent args)
    {
        var direction = EntityManager.TryGetComponent(uid, out PhysicsComponent? comp)
            ? comp.LinearVelocity / 50
            : Vector2.Zero;
        var dropAngle = Random.NextFloat(0.8f, 1.2f);

        var fellEvent = new FellDownEvent(uid);
        RaiseLocalEvent(uid, fellEvent);

        if (!TryComp(uid, out HandsComponent? handsComp))
            return;

        var worldRotation = _transform.GetWorldRotation(uid).ToVec();
        foreach (var hand in handsComp.Hands.Values)
        {
            if (hand.HeldEntity is not { } held)
                continue;

            if (!HasComp<KeepItemsOnFallComponent>(uid))
            {
                if (!_handsSystem.TryDrop(uid, hand, checkActionBlocker: false, handsComp: handsComp))
                    continue;
            }

            _throwingSystem.TryThrow(held,
                Random.NextAngle().RotateVec(direction / dropAngle + worldRotation / 50),
                0.5f * dropAngle * Random.NextFloat(-0.9f, 1.1f),
                uid, 0);
        }
    }
}

/// <summary>
/// Raised after an entity falls down.
/// </summary>
public sealed class FellDownEvent(EntityUid uid) : EntityEventArgs
{
    public EntityUid Uid { get; } = uid;
}