using System.Numerics;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Movement.Components;
using Content.Shared.Throwing;
using Content.Shared.White.Crossbow;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.Projectiles;

public abstract partial class SharedProjectileSystem : EntitySystem
{
    public const string ProjectileFixture = "projectile";

    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly PenetratedSystem _penetratedSystem = default!; // WD

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ProjectileComponent, PreventCollideEvent>(PreventCollision);
        SubscribeLocalEvent<EmbeddableProjectileComponent, ProjectileHitEvent>(OnEmbedProjectileHit);
        SubscribeLocalEvent<EmbeddableProjectileComponent, ThrowDoHitEvent>(OnEmbedThrowDoHit);
        SubscribeLocalEvent<EmbeddableProjectileComponent, ActivateInWorldEvent>(OnEmbedActivate);
        SubscribeLocalEvent<EmbeddableProjectileComponent, RemoveEmbeddedProjectileEvent>(OnEmbedRemove);
        SubscribeLocalEvent<EmbeddableProjectileComponent, AttemptPacifiedThrowEvent>(OnAttemptPacifiedThrow);

        SubscribeLocalEvent<EmbeddableProjectileComponent, LandEvent>(OnLand); // WD
        SubscribeLocalEvent<EmbeddableProjectileComponent, ComponentRemove>(OnRemove); // WD
        SubscribeLocalEvent<EmbeddableProjectileComponent, EntityTerminatingEvent>(OnEntityTerminating); // WD
    }

    private void OnEmbedActivate(EntityUid uid, EmbeddableProjectileComponent component, ActivateInWorldEvent args)
    {
        // WD EDIT START
        if (args.Handled || !TryComp<PhysicsComponent>(uid, out var physics) || physics.BodyType != BodyType.Static)
        {
            FreePenetrated(component);
            return;
        }

        args.Handled = true;

        if (!AttemptEmbedRemove(uid, args.User, component))
            FreePenetrated(component);
        // WD EDIT END
    }

    private void OnEmbedRemove(EntityUid uid, EmbeddableProjectileComponent component, RemoveEmbeddedProjectileEvent args)
    {
        // Whacky prediction issues.
        if (args.Cancelled || _netManager.IsClient)
            return;

        if (component.DeleteOnRemove)
        {
            QueueDel(uid);
            // WD START
            FreePenetrated(component);
            RaiseLocalEvent(uid, new EmbedRemovedEvent());
            // WD END
            return;
        }

        var xform = Transform(uid);
        TryComp<PhysicsComponent>(uid, out var physics);
        _physics.SetBodyType(uid, BodyType.Dynamic, body: physics, xform: xform);
        _transform.AttachToGridOrMap(uid, xform);

        // Reset whether the projectile has damaged anything if it successfully was removed
        if (TryComp<ProjectileComponent>(uid, out var projectile))
        {
            projectile.Shooter = null;
            projectile.Weapon = null;
            projectile.DamagedEntity = false;
        }

        // WD START
        FreePenetrated(component);
        RaiseLocalEvent(uid, new EmbedRemovedEvent());
        // WD END

        // Land it just coz uhhh yeah
        var landEv = new LandEvent(args.User, true);
        RaiseLocalEvent(uid, ref landEv);
        _physics.WakeBody(uid, body: physics);

        // try place it in the user's hand
        _hands.TryPickupAnyHand(args.User, uid);
    }

    private void OnEmbedThrowDoHit(EntityUid uid, EmbeddableProjectileComponent component, ThrowDoHitEvent args)
    {
        if (!component.EmbedOnThrow)
            return;

        // WD START
        if (component is {Penetrate: true, PenetratedUid: null} &&
            TryComp(args.Target, out PenetratedComponent? penetrated) &&
            penetrated is {ProjectileUid: null, IsPinned: false} &&
            TryComp(args.Target, out PhysicsComponent? physics))
        {
            component.PenetratedUid = args.Target;
            penetrated.ProjectileUid = uid;
            _physics.SetLinearVelocity(args.Target, Vector2.Zero, body: physics);
            _physics.SetBodyType(args.Target, BodyType.Static, body: physics);
            var xform = Transform(args.Target);
            _transform.SetParent(args.Target, xform, uid);
            _transform.SetLocalPosition(args.Target,
                xform.LocalPosition + Transform(uid).LocalRotation.RotateVec(new Vector2(0.5f, 0.5f)), xform);
            Dirty(uid, component);
            Dirty(args.Target, penetrated);
            return;
        }

        if (component.PenetratedUid == args.Target)
            args.Handled = true;
        else if (HasComp<MobMoverComponent>(args.Target) || HasComp<InputMoverComponent>(args.Target))
            FreePenetrated(component);
        // WD END

        Embed(uid, args.Target, component);
    }

    private void OnEmbedProjectileHit(EntityUid uid, EmbeddableProjectileComponent component, ref ProjectileHitEvent args)
    {
        Embed(uid, args.Target, component);

        // Raise a specific event for projectiles.
        if (TryComp<ProjectileComponent>(uid, out var projectile))
        {
            var ev = new ProjectileEmbedEvent(projectile.Shooter!.Value, projectile.Weapon!.Value, args.Target);
            RaiseLocalEvent(uid, ref ev);
        }
    }

    private void Embed(EntityUid uid, EntityUid target, EmbeddableProjectileComponent component)
    {
        if (component.PreventEmbedding || component.PenetratedUid == target) // WD START
            return;

        var ev = new EmbedStartEvent(component);
        RaiseLocalEvent(uid, ref ev);

        if (TryComp(component.PenetratedUid, out PenetratedComponent? penetrated))
            penetrated.IsPinned = true;
        // WD END

        TryComp<PhysicsComponent>(uid, out var physics);
        _physics.SetLinearVelocity(uid, Vector2.Zero, body: physics);
        _physics.SetBodyType(uid, BodyType.Static, body: physics);
        var xform = Transform(uid);
        _transform.SetParent(uid, xform, target);

        if (component.Offset != Vector2.Zero)
        {
            _transform.SetLocalPosition(xform, xform.LocalPosition + xform.LocalRotation.RotateVec(component.Offset));
        }

        if (component.Sound != null)
        {
            _audio.PlayPredicted(component.Sound, uid, null);
        }
    }

    private void PreventCollision(EntityUid uid, ProjectileComponent component, ref PreventCollideEvent args)
    {
        if (component.IgnoreShooter && (args.OtherEntity == component.Shooter || args.OtherEntity == component.Weapon))
        {
            args.Cancelled = true;
        }
    }

    public void SetShooter(EntityUid id, ProjectileComponent component, EntityUid shooterId)
    {
        if (component.Shooter == shooterId)
            return;

        component.Shooter = shooterId;
        Dirty(id, component);
    }

    [Serializable, NetSerializable]
    private sealed partial class RemoveEmbeddedProjectileEvent : DoAfterEvent
    {
        public override DoAfterEvent Clone() => this;
    }

    /// <summary>
    /// Prevent players with the Pacified status effect from throwing embeddable projectiles.
    /// </summary>
    private void OnAttemptPacifiedThrow(Entity<EmbeddableProjectileComponent> ent, ref AttemptPacifiedThrowEvent args)
    {
        args.Cancel("pacified-cannot-throw-embed");
    }

    // WD EDIT START
    private void OnEntityTerminating(EntityUid uid, EmbeddableProjectileComponent component,
        ref EntityTerminatingEvent args)
    {
        if (!_netManager.IsClient)
            FreePenetrated(component);
    }

    private void OnRemove(EntityUid uid, EmbeddableProjectileComponent component, ComponentRemove args)
    {
        if (!_netManager.IsClient)
            FreePenetrated(component);
    }

    private void FreePenetrated(EmbeddableProjectileComponent component)
    {
        if (component.PenetratedUid == null)
            return;

        _penetratedSystem.FreePenetrated(component.PenetratedUid.Value);
        component.PenetratedUid = null;
    }

    private void OnLand(EntityUid uid, EmbeddableProjectileComponent component, ref LandEvent args)
    {
        if (component.PenetratedUid == null)
            return;

        var penetratedUid = component.PenetratedUid.Value;
        component.PenetratedUid = null;

        _penetratedSystem.FreePenetrated(penetratedUid);

        Embed(uid, penetratedUid, component);
    }

    public bool AttemptEmbedRemove(EntityUid uid, EntityUid user, EmbeddableProjectileComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return false;

        // Nuh uh
        if (component.RemovalTime == null)
            return false;

        return _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, user, component.RemovalTime.Value,
            new RemoveEmbeddedProjectileEvent(), eventTarget: uid, target: uid)
        {
            DistanceThreshold = SharedInteractionSystem.InteractionRange,
        });
    }
    // WD EDIT END
}

[Serializable, NetSerializable]
public sealed class ImpactEffectEvent : EntityEventArgs
{
    public string Prototype;
    public NetCoordinates Coordinates;

    public ImpactEffectEvent(string prototype, NetCoordinates coordinates)
    {
        Prototype = prototype;
        Coordinates = coordinates;
    }
}

/// <summary>
/// Raised when an entity is just about to be hit with a projectile but can reflect it
/// </summary>
[ByRefEvent]
public record struct ProjectileReflectAttemptEvent(EntityUid ProjUid, ProjectileComponent Component, bool Cancelled);

/// <summary>
/// Raised when a projectile hits an entity
/// </summary>
[ByRefEvent]
public record struct ProjectileHitEvent(DamageSpecifier Damage, EntityUid Target, EntityUid? Shooter = null);
