using System.Linq;
using Content.Shared.Eye;
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

            _physics.SetCollisionMask(uid, fixture.Key, fixture.Value, (int) CollisionGroup.GhostImpassable, fixtures);
            _physics.SetCollisionLayer(uid, fixture.Key, fixture.Value, 0, fixtures);
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
        _movement.RefreshMovementSpeedModifiers(uid);
    }

    private void OnComponentRemoved(EntityUid uid, IncorporealComponent component, ComponentShutdown args)
    {
        if (TryComp<FixturesComponent>(uid, out var fixtures) && fixtures.FixtureCount >= 1)
        {
            var fixture = fixtures.Fixtures.First();

            _physics.SetCollisionMask(uid, fixture.Key, fixture.Value, (int) (CollisionGroup.MobMask | CollisionGroup.GhostImpassable), fixtures);
            _physics.SetCollisionLayer(uid, fixture.Key, fixture.Value, (int) CollisionGroup.MobLayer, fixtures);
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
