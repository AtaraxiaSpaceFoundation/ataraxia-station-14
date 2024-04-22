using System.Threading;
using Content.Server.DoAfter;
using Content.Server.Inventory;
using Content.Server.Popups;
using Content.Server.Resist;
using Content.Shared._White.Carrying;
using Content.Shared._White.Crossbow;
using Content.Shared.ActionBlocker;
using Content.Shared.Buckle.Components;
using Content.Shared.Climbing.Events;
using Content.Shared.DoAfter;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Standing.Systems;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Map.Components;

namespace Content.Server._White.Carrying;

public sealed class CarryingSystem : EntitySystem
{
    [Dependency] private readonly VirtualItemSystem _virtualItemSystem = default!;
    [Dependency] private readonly CarryingSlowdownSystem _slowdown = default!;
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
    [Dependency] private readonly PullingSystem _pullingSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly EscapeInventorySystem _escapeInventorySystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly PenetratedSystem _penetrated = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CarriableComponent, GetVerbsEvent<AlternativeVerb>>(AddCarryVerb);
        SubscribeLocalEvent<CarryingComponent, VirtualItemDeletedEvent>(OnVirtualItemDeleted);
        SubscribeLocalEvent<CarryingComponent, BeforeThrowEvent>(OnThrow);
        SubscribeLocalEvent<CarryingComponent, EntParentChangedMessage>(OnParentChanged);
        SubscribeLocalEvent<CarryingComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<BeingCarriedComponent, InteractionAttemptEvent>(OnInteractionAttempt);
        SubscribeLocalEvent<BeingCarriedComponent, MoveInputEvent>(OnMoveInput);
        SubscribeLocalEvent<BeingCarriedComponent, UpdateCanMoveEvent>(OnMoveAttempt);
        SubscribeLocalEvent<BeingCarriedComponent, StandAttemptEvent>(OnStandAttempt);
        SubscribeLocalEvent<BeingCarriedComponent, GettingInteractedWithAttemptEvent>(OnInteractedWith);
        SubscribeLocalEvent<BeingCarriedComponent, PullAttemptEvent>(OnPullAttempt);
        SubscribeLocalEvent<BeingCarriedComponent, StartClimbEvent>(OnStartClimb);
        SubscribeLocalEvent<BeingCarriedComponent, BuckleChangeEvent>(OnBuckleChange);
        SubscribeLocalEvent<CarriableComponent, CarryDoAfterEvent>(OnDoAfter);
    }

    private void AddCarryVerb(EntityUid uid, CarriableComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (!CanCarry(args.User, uid, component))
            return;

        if (HasComp<CarryingComponent>(args.User)) // yeah not dealing with that
            return;

        if (HasComp<BeingCarriedComponent>(args.User) || HasComp<BeingCarriedComponent>(args.Target))
            return;

        if (!_mobStateSystem.IsAlive(args.User))
            return;

        if (args.User == args.Target)
            return;

        AlternativeVerb verb = new()
        {
            Act = () =>
            {
                StartCarryDoAfter(args.User, uid, component);
            },
            Text = Loc.GetString("carry-verb"),
            Priority = 2
        };

        args.Verbs.Add(verb);
    }

    /// <summary>
    /// Since the carried entity is stored as 2 virtual items, when deleted we want to drop them.
    /// </summary>
    private void OnVirtualItemDeleted(EntityUid uid, CarryingComponent component, VirtualItemDeletedEvent args)
    {
        if (!HasComp<CarriableComponent>(args.BlockingEntity))
            return;

        DropCarried(uid, args.BlockingEntity);
    }

    /// <summary>
    /// Basically using virtual item passthrough to throw the carried person. A new age!
    /// Maybe other things besides throwing should use virt items like this...
    /// </summary>
    private void OnThrow(EntityUid uid, CarryingComponent component, ref BeforeThrowEvent args)
    {
        if (!TryComp<VirtualItemComponent>(args.ItemUid, out var virtItem) ||
            !HasComp<CarriableComponent>(virtItem.BlockingEntity))
            return;

        args.ItemUid = virtItem.BlockingEntity;
    }

    private void OnParentChanged(EntityUid uid, CarryingComponent component, ref EntParentChangedMessage args)
    {
        if (Transform(uid).MapID != args.OldMapId)
            return;

        DropCarried(uid, component.Carried);
    }

    private void OnMobStateChanged(EntityUid uid, CarryingComponent component, MobStateChangedEvent args)
    {
        DropCarried(uid, component.Carried);
    }

    /// <summary>
    /// Only let the person being carried interact with their carrier and things on their person.
    /// </summary>
    private void OnInteractionAttempt(EntityUid uid, BeingCarriedComponent component, InteractionAttemptEvent args)
    {
        if (args.Target == null)
            return;

        var targetParent = Transform(args.Target.Value).ParentUid;

        if (args.Target.Value != component.Carrier && targetParent != component.Carrier && targetParent != uid)
            args.Cancel();
    }

    /// <summary>
    /// Try to escape via the escape inventory system.
    /// </summary>
    private void OnMoveInput(EntityUid uid, BeingCarriedComponent component, ref MoveInputEvent args)
    {
        if (!TryComp<CanEscapeInventoryComponent>(uid, out var escape))
            return;

        if (_actionBlockerSystem.CanInteract(uid, component.Carrier))
        {
            _escapeInventorySystem.AttemptEscape(uid, component.Carrier, escape);
        }
    }

    private void OnMoveAttempt(EntityUid uid, BeingCarriedComponent component, UpdateCanMoveEvent args)
    {
        args.Cancel();
    }

    private void OnStandAttempt(EntityUid uid, BeingCarriedComponent component, StandAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnInteractedWith(
        EntityUid uid,
        BeingCarriedComponent component,
        GettingInteractedWithAttemptEvent args)
    {
        if (args.Uid != component.Carrier)
            args.Cancel();
    }

    private void OnPullAttempt(EntityUid uid, BeingCarriedComponent component, PullAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnStartClimb(EntityUid uid, BeingCarriedComponent component, StartClimbEvent args)
    {
        DropCarried(component.Carrier, uid);
    }

    private void OnBuckleChange(EntityUid uid, BeingCarriedComponent component, ref BuckleChangeEvent args)
    {
        DropCarried(component.Carrier, uid);
    }

    private void OnDoAfter(EntityUid uid, CarriableComponent component, CarryDoAfterEvent args)
    {
        component.CancelToken = null;
        if (args.Handled || args.Cancelled)
            return;

        if (!CanCarry(args.Args.User, uid, component))
            return;

        Carry(args.Args.User, uid);
        args.Handled = true;
    }

    private void StartCarryDoAfter(EntityUid carrier, EntityUid carried, CarriableComponent component)
    {
        var length = TimeSpan.FromSeconds(6); // т.к. удалили систему разницы масс увеличу время с 3 до 6
        if (length >= TimeSpan.FromSeconds(9))
        {
            _popupSystem.PopupEntity(Loc.GetString("carry-too-heavy"), carried, carrier,
                Shared.Popups.PopupType.SmallCaution);

            return;
        }

        if (!HasComp<KnockedDownComponent>(carried))
            length *= 2f;

        component.CancelToken = new CancellationTokenSource();

        var ev = new CarryDoAfterEvent();
        var args = new DoAfterArgs(EntityManager, carrier, length, ev, carried, target: carried)
        {
            BreakOnMove = true,
            NeedHand = true
        };

        _doAfterSystem.TryStartDoAfter(args);
    }

    private void Carry(EntityUid carrier, EntityUid carried)
    {
        if (TryComp<PullableComponent>(carried, out var pullable))
            _pullingSystem.TryStopPull(carried, pullable);

        _penetrated.FreePenetrated(carried);

        _transform.AttachToGridOrMap(carrier);
        _transform.AttachToGridOrMap(carried);
        _transform.SetCoordinates(carried, Transform(carrier).Coordinates);
        _transform.SetParent(carried, carrier);

        _virtualItemSystem.TrySpawnVirtualItemInHand(carried, carrier);
        _virtualItemSystem.TrySpawnVirtualItemInHand(carried, carrier);

        var carryingComp = EnsureComp<CarryingComponent>(carrier);
        ApplyCarrySlowdown(carrier);
        var carriedComp = EnsureComp<BeingCarriedComponent>(carried);
        EnsureComp<KnockedDownComponent>(carried);
        EnsureComp<CanEscapeInventoryComponent>(carried);

        carryingComp.Carried = carried;
        carriedComp.Carrier = carrier;

        _actionBlockerSystem.UpdateCanMove(carried);
    }

    public void DropCarried(EntityUid carrier, EntityUid carried)
    {
        RemComp<CarryingComponent>(carrier); // get rid of this first so we don't recusrively fire that event
        RemComp<CarryingSlowdownComponent>(carrier);
        RemComp<BeingCarriedComponent>(carried);
        RemComp<KnockedDownComponent>(carried);
        RemComp<CanEscapeInventoryComponent>(carried);

        _actionBlockerSystem.UpdateCanMove(carried);
        _virtualItemSystem.DeleteInHandsMatching(carrier, carried);
        _transform.AttachToGridOrMap(carried);
        _movementSpeed.RefreshMovementSpeedModifiers(carrier);
    }

    private void ApplyCarrySlowdown(EntityUid carrier)
    {
        var slowdownComp = EnsureComp<CarryingSlowdownComponent>(carrier);
        _slowdown.SetModifier(carrier, slowdownComp);
    }

    public bool CanCarry(EntityUid carrier, EntityUid carried, CarriableComponent? carriedComp = null)
    {
        if (!Resolve(carried, ref carriedComp, false))
            return false;

        if (carriedComp.CancelToken != null)
            return false;

        if (!HasComp<MapGridComponent>(Transform(carrier).ParentUid))
            return false;

        if (HasComp<BeingCarriedComponent>(carrier) || HasComp<BeingCarriedComponent>(carried))
            return false;

        if (!TryComp<HandsComponent>(carrier, out var hands))
            return false;

        if (hands.CountFreeHands() < carriedComp.FreeHandsRequired)
            return false;

        return true;
    }
}