using Content.Shared.DoAfter;
using Content.Shared.Gravity;
using Content.Shared.Hands.Components;
using Content.Shared.Input;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Physics;
using Content.Shared.Rotation;
using Content.Shared.Slippery;
using Content.Shared.Stunnable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Input.Binding;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Serialization;

namespace Content.Shared.Standing.Systems;

public abstract partial class SharedStandingStateSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!; // WD EDIT
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!; // WD EDIT
    [Dependency] private readonly SharedStunSystem _stun = default!; // WD EDIT
    [Dependency] private readonly MobStateSystem _mobState = default!; // WD EDIT

    // If StandingCollisionLayer value is ever changed to more than one layer, the logic needs to be edited.
    private const int StandingCollisionLayer = (int)CollisionGroup.MidImpassable;

    // WD EDIT START

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.LieDown, InputCmdHandler.FromDelegate(ChangeLyingState))
            .Register<SharedStandingStateSystem>();

        SubscribeNetworkEvent<ChangeStandingStateEvent>(OnChangeState);

        SubscribeLocalEvent<StandingStateComponent, StandingUpDoAfterEvent>(OnStandingUpDoAfter);
        SubscribeLocalEvent<StandingStateComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeed);
        SubscribeLocalEvent<StandingStateComponent, SlipAttemptEvent>(OnSlipAttempt);

        InitializeColliding();
    }

    #region Events

    private void OnChangeState(ChangeStandingStateEvent ev, EntitySessionEventArgs args)
    {
        if (!args.SenderSession.AttachedEntity.HasValue)
        {
            return;
        }

        var uid = args.SenderSession.AttachedEntity.Value;

        if (_stun.IsParalyzed(uid))
        {
            return;
        }

        if (!_mobState.IsAlive(uid))
        {
            return;
        }

        if (IsDown(uid))
        {
            TryStandUp(uid);
        }
        else
        {
            TryLieDown(uid);
        }
    }

    private void OnStandingUpDoAfter(EntityUid uid, StandingStateComponent component, StandingUpDoAfterEvent args)
    {
        Stand(uid);
    }

    private void OnRefreshMovementSpeed(EntityUid uid, StandingStateComponent component,
        RefreshMovementSpeedModifiersEvent args)
    {
        if (IsDown(uid))
            args.ModifySpeed(0.4f, 0.4f);
        else
            args.ModifySpeed(1f, 1f);
    }

    private void OnSlipAttempt(EntityUid uid, StandingStateComponent component, SlipAttemptEvent args)
    {
        if (IsDown(uid))
        {
            args.Cancel();
        }
    }

    #endregion

    #region Methods

    /// <summary>
    ///     Send an update event when player pressed keybind.
    /// </summary>
    private void ChangeLyingState(ICommonSession? session)
    {
        if (session?.AttachedEntity == null ||
            !TryComp(session.AttachedEntity, out StandingStateComponent? standing) ||
            !standing.CanLieDown)
        {
            return;
        }

        RaiseNetworkEvent(new ChangeStandingStateEvent());
    }

    public bool TryStandUp(EntityUid uid, StandingStateComponent? standingState = null)
    {
        if (!Resolve(uid, ref standingState, false))
            return false;

        if (standingState.CurrentState is not StandingState.Lying)
            return false;

        var doargs = new DoAfterArgs(EntityManager, uid, standingState.StandingUpTime,
            new StandingUpDoAfterEvent(), uid)
        {
            BreakOnMove = false,
            BreakOnDamage = false,
            BreakOnHandChange = false,
            RequireCanInteract = false
        };

        if (!_doAfter.TryStartDoAfter(doargs))
            return false;

        standingState.CurrentState = StandingState.GettingUp;
        return true;
    }

    public bool TryLieDown(EntityUid uid, StandingStateComponent? standingState = null, bool dropHeldItems = false)
    {
        if (!Resolve(uid, ref standingState, false) || !standingState.CanLieDown)
            return false;

        if (standingState.CurrentState is not StandingState.Standing)
        {
            return false;
        }

        Down(uid, true, dropHeldItems, standingState);
        return true;
    }
    // WD EDIT END

    public bool IsDown(EntityUid uid, StandingStateComponent? standingState = null)
    {
        if (!Resolve(uid, ref standingState, false))
            return false;

        return standingState.CurrentState is StandingState.Lying or StandingState.GettingUp;
    }

    public bool Down(
        EntityUid uid,
        bool playSound = true,
        bool dropHeldItems = true,
        StandingStateComponent? standingState = null,
        AppearanceComponent? appearance = null,
        HandsComponent? hands = null)
    {
        if (!Resolve(uid, ref standingState, false))
            return false;

        // Optional component.
        Resolve(uid, ref appearance, ref hands, false);

        // This is just to avoid most callers doing this manually saving boilerplate
        // 99% of the time you'll want to drop items but in some scenarios (e.g. buckling) you don't want to.
        // We do this BEFORE downing because something like buckle may be blocking downing but we want to drop hand items anyway
        // and ultimately this is just to avoid boilerplate in Down callers + keep their behavior consistent.
        if (dropHeldItems && hands != null)
        {
            RaiseLocalEvent(uid, new DropHandItemsEvent());
        }

        if (standingState.CurrentState is StandingState.Lying or StandingState.GettingUp)
            return true;

        var msg = new DownAttemptEvent();
        RaiseLocalEvent(uid, msg);

        if (msg.Cancelled)
            return false;

        standingState.CurrentState = StandingState.Lying;
        Dirty(uid, standingState);
        RaiseLocalEvent(uid, new DownedEvent());

        // Seemed like the best place to put it
        _appearance.SetData(uid, RotationVisuals.RotationState, RotationState.Horizontal, appearance);

        // Change collision masks to allow going under certain entities like flaps and tables
        if (TryComp(uid, out FixturesComponent? fixtureComponent))
        {
            foreach (var (key, fixture) in fixtureComponent.Fixtures)
            {
                if ((fixture.CollisionMask & StandingCollisionLayer) == 0)
                    continue;

                standingState.ChangedFixtures.Add(key);
                _physics.SetCollisionMask(uid, key, fixture, fixture.CollisionMask & ~StandingCollisionLayer,
                    manager: fixtureComponent);
            }
        }

        // check if component was just added or streamed to client
        // if true, no need to play sound - mob was down before player could seen that
        if (standingState.LifeStage <= ComponentLifeStage.Starting)
            return true;

        if (playSound)
        {
            _audio.PlayPredicted(standingState.DownSound, uid, null);
        }

        _movement.RefreshMovementSpeedModifiers(uid);
        return true;
    }

    public bool Stand(
        EntityUid uid,
        StandingStateComponent? standingState = null,
        AppearanceComponent? appearance = null,
        bool force = false)
    {
        if (!Resolve(uid, ref standingState, false))
            return false;

        // Optional component.
        Resolve(uid, ref appearance, false);

        if (standingState.CurrentState is StandingState.Standing)
            return true;

        if (!force)
        {
            var msg = new StandAttemptEvent();
            RaiseLocalEvent(uid, msg);

            if (msg.Cancelled)
                return false;
        }

        standingState.CurrentState = StandingState.Standing;
        Dirty(uid, standingState);
        RaiseLocalEvent(uid, new StoodEvent());

        _appearance.SetData(uid, RotationVisuals.RotationState, RotationState.Vertical, appearance);

        if (TryComp(uid, out FixturesComponent? fixtureComponent))
        {
            foreach (var key in standingState.ChangedFixtures)
            {
                if (fixtureComponent.Fixtures.TryGetValue(key, out var fixture))
                {
                    _physics.SetCollisionMask(uid, key, fixture, fixture.CollisionMask | StandingCollisionLayer,
                        fixtureComponent);
                }
            }
        }

        standingState.ChangedFixtures.Clear();
        _movement.RefreshMovementSpeedModifiers(uid);

        return true;
    }

    #endregion
}

public sealed class DropHandItemsEvent : EventArgs;

/// <summary>
/// Subscribe if you can potentially block a down attempt.
/// </summary>
public sealed class DownAttemptEvent : CancellableEntityEventArgs;

/// <summary>
/// Subscribe if you can potentially block a stand attempt.
/// </summary>
public sealed class StandAttemptEvent : CancellableEntityEventArgs;

/// <summary>
/// Raised when an entity becomes standing
/// </summary>
public sealed class StoodEvent : EntityEventArgs;

/// <summary>
/// Raised when an entity is not standing
/// </summary>
public sealed class DownedEvent : EntityEventArgs;

// WD EDIT
[Serializable, NetSerializable]
public sealed partial class StandingUpDoAfterEvent : SimpleDoAfterEvent;
