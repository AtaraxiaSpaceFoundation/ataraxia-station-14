using System.Linq;
using System.Numerics;
using Content.Shared.ActionBlocker;
using Content.Shared.Emoting;
using Content.Shared.Hands;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Movement.Events;
using Content.Shared.Speech;
using Content.Shared.Throwing;
using Robust.Shared.Containers;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Spawners;

namespace Content.Shared._White.Wizard.Timestop;

public sealed class FreezeContactsSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FreezeContactsComponent, StartCollideEvent>(OnEntityEnter);
        SubscribeLocalEvent<FreezeContactsComponent, EndCollideEvent>(OnEntityExit);
        SubscribeLocalEvent<FrozenComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<FrozenComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<FrozenComponent, PreventCollideEvent>(OnPreventCollide);
        SubscribeLocalEvent<FrozenComponent, EntGotInsertedIntoContainerMessage>(OnGetInserted);

        SubscribeLocalEvent<FrozenComponent, SpeakAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<FrozenComponent, EmoteAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<FrozenComponent, ChangeDirectionAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<FrozenComponent, UpdateCanMoveEvent>(OnMoveAttempt);
        SubscribeLocalEvent<FrozenComponent, InteractionAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<FrozenComponent, UseAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<FrozenComponent, ThrowAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<FrozenComponent, DropAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<FrozenComponent, AttackAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<FrozenComponent, PickupAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<FrozenComponent, IsEquippingAttemptEvent>(OnEquipAttempt);
        SubscribeLocalEvent<FrozenComponent, IsUnequippingAttemptEvent>(OnUnequipAttempt);
    }

    private void OnMoveAttempt(EntityUid uid, FrozenComponent component, UpdateCanMoveEvent args)
    {
        if (component.LifeStage > ComponentLifeStage.Running)
            return;

        args.Cancel();
    }

    private void OnAttempt(EntityUid uid, FrozenComponent component, CancellableEntityEventArgs args)
    {
        args.Cancel();
    }

    private void OnEquipAttempt(EntityUid uid, FrozenComponent component, IsEquippingAttemptEvent args)
    {
        // is this a self-equip, or are they being stripped?
        if (args.Equipee == uid)
            args.Cancel();
    }

    private void OnUnequipAttempt(EntityUid uid, FrozenComponent component, IsUnequippingAttemptEvent args)
    {
        // is this a self-equip, or are they being stripped?
        if (args.Unequipee == uid)
            args.Cancel();
    }

    private void OnGetInserted(Entity<FrozenComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        RemCompDeferred<FrozenComponent>(ent);
    }

    private void OnPreventCollide(Entity<FrozenComponent> ent, ref PreventCollideEvent args)
    {
        if (args.OurBody.BodyType == BodyType.Dynamic && !HasComp<FreezeContactsComponent>(args.OtherEntity))
            args.Cancelled = true;
    }

    private void OnRemove(Entity<FrozenComponent> ent, ref ComponentRemove args)
    {
        var (uid, comp) = ent;

        _blocker.UpdateCanMove(uid);

        if (_container.IsEntityOrParentInContainer(uid))
            return;

        if (!TryComp(uid, out PhysicsComponent? physics))
            return;

        _physics.SetLinearVelocity(uid, comp.OldLinearVelocity, false, body: physics);
        _physics.SetAngularVelocity(uid, comp.OldAngularVelocity, body: physics);
    }

    private void OnInit(Entity<FrozenComponent> ent, ref ComponentInit args)
    {
        var (uid, comp) = ent;

        _blocker.UpdateCanMove(uid);

        if (!TryComp(uid, out PhysicsComponent? physics))
            return;

        comp.OldLinearVelocity = physics.LinearVelocity;
        comp.OldAngularVelocity = physics.AngularVelocity;

        _physics.SetLinearVelocity(uid, Vector2.Zero, false, body: physics);
        _physics.SetAngularVelocity(uid, 0f, body: physics);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = AllEntityQuery<FrozenComponent, FixturesComponent, PhysicsComponent>();

        while (query.MoveNext(out var uid, out var frozen, out var fixtures, out var physics))
        {
            frozen.Lifetime -= frameTime;

            if (physics.LinearVelocity != Vector2.Zero)
                _physics.SetLinearVelocity(uid, Vector2.Zero, manager: fixtures, body: physics);

            if (physics.AngularVelocity != 0f)
                _physics.SetAngularVelocity(uid, 0f, manager: fixtures, body: physics);

            if (frozen.Lifetime > 0)
                continue;

            RemCompDeferred<FrozenComponent>(uid);
        }
    }

    private void OnEntityExit(Entity<FreezeContactsComponent> ent, ref EndCollideEvent args)
    {
        if (IsTouchingFrozenContacts(args.OtherEntity, args.OtherBody))
            return;

        RemCompDeferred<FrozenComponent>(args.OtherEntity);
    }

    private void OnEntityEnter(Entity<FreezeContactsComponent> ent, ref StartCollideEvent args)
    {
        var hadFrozen = HasComp<FrozenComponent>(args.OtherEntity);
        var frozen = EnsureComp<FrozenComponent>(args.OtherEntity);

        if (!TryComp(ent, out TimedDespawnComponent? timedDespawn))
            return;

        frozen.Lifetime = timedDespawn.Lifetime;

        if (TryComp(args.OtherEntity, out TimedDespawnComponent? otherTimedDespawn))
            otherTimedDespawn.Lifetime += timedDespawn.Lifetime;

        if (hadFrozen)
            return;

        if (!TryComp(args.OtherEntity, out ThrownItemComponent? thrownItem))
            return;

        if (thrownItem.LandTime != null)
            thrownItem.LandTime = thrownItem.LandTime.Value + TimeSpan.FromSeconds(timedDespawn.Lifetime);

        if (thrownItem.ThrownTime != null)
            thrownItem.ThrownTime = thrownItem.ThrownTime.Value + TimeSpan.FromSeconds(timedDespawn.Lifetime);
    }

    private bool IsTouchingFrozenContacts(EntityUid uid, PhysicsComponent body)
    {
        return _physics.GetContactingEntities(uid, body).Any(HasComp<FreezeContactsComponent>);
    }
}
