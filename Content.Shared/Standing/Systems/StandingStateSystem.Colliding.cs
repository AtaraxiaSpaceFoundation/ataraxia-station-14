using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;

namespace Content.Shared.Standing.Systems;

// WD ADDED
public abstract partial class SharedStandingStateSystem
{
    [Dependency] protected readonly IRobustRandom Random = default!;

    private void InitializeColliding()
    {
        SubscribeLocalEvent<StandingStateComponent, ProjectileCollideAttemptEvent>(OnProjectileCollideAttempt);
        SubscribeLocalEvent<StandingStateComponent, HitscanHitAttemptEvent>(OnHitscanHitAttempt);
    }

    private void OnProjectileCollideAttempt(EntityUid uid, StandingStateComponent component,
        ref ProjectileCollideAttemptEvent args)
    {
        if (component.CurrentState is StandingState.Standing)
        {
            return;
        }

        if (!TryHit(uid, args.Component.Target, args.Component.IgnoreTarget))
        {
            args.Cancelled = true;
        }
    }

    private void OnHitscanHitAttempt(EntityUid uid, StandingStateComponent component, ref HitscanHitAttemptEvent args)
    {
        if (component.CurrentState is StandingState.Standing)
        {
            return;
        }

        if (!TryHit(uid, args.Target))
        {
            args.Cancelled = true;
        }
    }

    private bool TryHit(EntityUid uid, EntityUid? target, bool ignoreTarget = false)
    {
        // Lying and being pulled
        if (!ignoreTarget && TryComp(uid, out PullableComponent? pullable) && pullable.BeingPulled)
            return uid == target;

        if (!TryComp(uid, out PhysicsComponent? physics))
            return true;

        // If alive and moving
        if (_mobState.IsAlive(uid) && (ignoreTarget || physics.LinearVelocity.LengthSquared() > 0.01f))
        {
            // We should hit
            return true;
        }

        // Only hit if we're target
        return uid == target;
    }
}
