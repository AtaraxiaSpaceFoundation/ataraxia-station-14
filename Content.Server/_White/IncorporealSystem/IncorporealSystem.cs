using System.Linq;
using Content.Shared.Eye;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Physics;
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;

namespace Content.Server._White.IncorporealSystem;

public sealed class IncorporealSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly VisibilitySystem _visibilitySystem = default!;
    [Dependency] private readonly SharedStealthSystem _stealth = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<IncorporealComponent, ComponentStartup>(OnComponentInit);
        SubscribeLocalEvent<IncorporealComponent, ComponentShutdown>(OnComponentRemoved);
        SubscribeLocalEvent<IncorporealComponent, RefreshMovementSpeedModifiersEvent>(OnRefresh);
    }

    private void OnComponentInit(EntityUid uid, IncorporealComponent component, ComponentStartup args)
    {
        if (TryComp<FixturesComponent>(uid, out var fixtures) && fixtures.FixtureCount >= 1)
        {
            var fixture = fixtures.Fixtures.First();

            component.StoredMask = fixture.Value.CollisionMask;
            component.StoredLayer = fixture.Value.CollisionLayer;

            _physics.SetCollisionMask(uid, fixture.Key, fixture.Value, component.CollisionMask, fixtures);
            _physics.SetCollisionLayer(uid, fixture.Key, fixture.Value, component.CollisionLayer, fixtures);
        }

        if (TryComp<VisibilityComponent>(uid, out var visibility))
        {
            _visibilitySystem.AddLayer((uid, visibility), (int) VisibilityFlags.Ghost, false);
            _visibilitySystem.RemoveLayer((uid, visibility), (int) VisibilityFlags.Normal, false);
            _visibilitySystem.RefreshVisibility(uid);
        }

        Spawn("EffectEmpPulse", Transform(uid).Coordinates);
        EnsureComp<StealthComponent>(uid);
        _stealth.SetVisibility(uid, -1);
        if (TryComp(uid, out PullableComponent? pullable))
            _pulling.TryStopPull(uid, pullable);
        _movement.RefreshMovementSpeedModifiers(uid);
    }

    private void OnComponentRemoved(EntityUid uid, IncorporealComponent component, ComponentShutdown args)
    {
        if (TryComp<FixturesComponent>(uid, out var fixtures) && fixtures.FixtureCount >= 1)
        {
            var fixture = fixtures.Fixtures.First();

            _physics.SetCollisionMask(uid, fixture.Key, fixture.Value, component.StoredMask, fixtures);
            _physics.SetCollisionLayer(uid, fixture.Key, fixture.Value, component.StoredLayer, fixtures);
        }

        if (TryComp<VisibilityComponent>(uid, out var visibility))
        {
            _visibilitySystem.RemoveLayer((uid, visibility), (int) VisibilityFlags.Ghost, false);
            _visibilitySystem.AddLayer((uid, visibility), (int) VisibilityFlags.Normal, false);
            _visibilitySystem.RefreshVisibility(uid);
        }

        component.MovementSpeedBuff = 1;
        Spawn("EffectEmpPulse", _transform.GetMapCoordinates(uid));
        _stealth.SetVisibility(uid, 1);
        RemComp<StealthComponent>(uid);
        _movement.RefreshMovementSpeedModifiers(uid);
    }

    private void OnRefresh(EntityUid uid, IncorporealComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(component.MovementSpeedBuff, component.MovementSpeedBuff);
    }
}
