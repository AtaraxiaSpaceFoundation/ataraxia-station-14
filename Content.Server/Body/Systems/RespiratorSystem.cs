using Content.Server.Administration.Logs;
using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Components;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.DoAfter;
using Content.Server.Nutrition.Components; // WD
using Content.Server.Popups;
using Content.Shared.ActionBlocker; // WD
using Content.Shared.Alert;
using Content.Shared.Atmos;
using Content.Shared.Body.Components;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid; // WD
using Content.Shared.IdentityManagement; // WD
using Content.Shared.Interaction; // WD
using Content.Shared.Inventory; // WD
using Content.Shared.Mobs; // WD
using Content.Shared.Mobs.Components; // WD
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups; // WD
using Content.Shared._White.CPR.Events;
using Content.Shared._White.Mood;
using Content.Shared.Changeling; // WD
using JetBrains.Annotations;
using Robust.Server.Audio; // WD
using Robust.Shared.Audio; // WD
// WD removed
using Robust.Shared.Timing;

namespace Content.Server.Body.Systems;

[UsedImplicitly]
public sealed class RespiratorSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;
    [Dependency] private readonly AtmosphereSystem _atmosSys = default!;
    [Dependency] private readonly BodySystem _bodySystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSys = default!;
    [Dependency] private readonly LungSystem _lungSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!; // WD
    [Dependency] private readonly ActionBlockerSystem _blocker = default!; // WD
    [Dependency] private readonly AudioSystem _audio = default!; // WD
    [Dependency] private readonly DoAfterSystem _doAfter = default!; // WD
    [Dependency] private readonly DamageableSystem _damageable = default!; // WD

    public override void Initialize()
    {
        base.Initialize();

        // We want to process lung reagents before we inhale new reagents.
        UpdatesAfter.Add(typeof(MetabolizerSystem));
        SubscribeLocalEvent<RespiratorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RespiratorComponent, InteractHandEvent>(OnHandInteract); // WD
        SubscribeLocalEvent<RespiratorComponent, CPREndedEvent>(OnCPRDoAfterEnd); // WD
        SubscribeLocalEvent<RespiratorComponent, EntityUnpausedEvent>(OnUnpaused);
        SubscribeLocalEvent<RespiratorComponent, ApplyMetabolicMultiplierEvent>(OnApplyMetabolicMultiplier);
    }

    private void OnMapInit(Entity<RespiratorComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUpdate = _gameTiming.CurTime + ent.Comp.UpdateInterval;
    }

    private void OnUnpaused(Entity<RespiratorComponent> ent, ref EntityUnpausedEvent args)
    {
        ent.Comp.NextUpdate += args.PausedTime;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<RespiratorComponent, BodyComponent>();
        while (query.MoveNext(out var uid, out var respirator, out var body))
        {
            if (_gameTiming.CurTime < respirator.NextUpdate)
                continue;

            respirator.NextUpdate += respirator.UpdateInterval;

            if (_mobState.IsDead(uid))
                continue;

            UpdateSaturation(uid, -(float) respirator.UpdateInterval.TotalSeconds, respirator);

            if (!_mobState.IsIncapacitated(uid)) // cannot breathe in crit.
            {
                switch (respirator.Status)
                {
                    case RespiratorStatus.Inhaling:
                        Inhale(uid, body);
                        respirator.Status = RespiratorStatus.Exhaling;
                        break;
                    case RespiratorStatus.Exhaling:
                        Exhale(uid, body);
                        respirator.Status = RespiratorStatus.Inhaling;
                        break;
                }
            }

            if (respirator.Saturation < respirator.SuffocationThreshold)
            {
                if (TryComp(uid, out VoidAdaptationComponent? voidAdaptation))
                {
                    voidAdaptation.ChemMultiplier = 0.75f;
                    StopSuffocation((uid, respirator));
                    respirator.SuffocationCycles = 0;
                    continue;
                }

                if (_gameTiming.CurTime >= respirator.LastGaspPopupTime + respirator.GaspPopupCooldown)
                {
                    respirator.LastGaspPopupTime = _gameTiming.CurTime;
                    _popupSystem.PopupEntity($"{Name(Identity.Entity(uid, EntityManager))} задыхается!", uid);
                }

                TakeSuffocationDamage((uid, respirator));
                respirator.SuffocationCycles += 1;
                continue;
            }

            StopSuffocation((uid, respirator));
            respirator.SuffocationCycles = 0;
        }
    }

    public void Inhale(EntityUid uid, BodyComponent? body = null)
    {
        if (!Resolve(uid, ref body, logMissing: false))
            return;

        var organs = _bodySystem.GetBodyOrganComponents<LungComponent>(uid, body);

        // Inhale gas
        var ev = new InhaleLocationEvent();
        RaiseLocalEvent(uid, ref ev, broadcast: false);

        ev.Gas ??= _atmosSys.GetContainingMixture(uid, excite: true);

        if (ev.Gas is null)
        {
            return;
        }

        var actualGas = ev.Gas.RemoveVolume(Atmospherics.BreathVolume);

        var lungRatio = 1.0f / organs.Count;
        var gas = organs.Count == 1 ? actualGas : actualGas.RemoveRatio(lungRatio);
        foreach (var (lung, _) in organs)
        {
            // Merge doesn't remove gas from the giver.
            _atmosSys.Merge(lung.Air, gas);
            _lungSystem.GasToReagent(lung.Owner, lung);
        }
    }

    public void Exhale(EntityUid uid, BodyComponent? body = null)
    {
        if (!Resolve(uid, ref body, logMissing: false))
            return;

        var organs = _bodySystem.GetBodyOrganComponents<LungComponent>(uid, body);

        // exhale gas

        var ev = new ExhaleLocationEvent();
        RaiseLocalEvent(uid, ref ev, broadcast: false);

        if (ev.Gas is null)
        {
            ev.Gas = _atmosSys.GetContainingMixture(uid, excite: true);

            // Walls and grids without atmos comp return null. I guess it makes sense to not be able to exhale in walls,
            // but this also means you cannot exhale on some grids.
            ev.Gas ??= GasMixture.SpaceGas;
        }

        var outGas = new GasMixture(ev.Gas.Volume);
        foreach (var (lung, _) in organs)
        {
            _atmosSys.Merge(outGas, lung.Air);
            lung.Air.Clear();

            if (_solutionContainerSystem.ResolveSolution(lung.Owner, lung.SolutionName, ref lung.Solution))
                _solutionContainerSystem.RemoveAllSolution(lung.Solution.Value);
        }

        _atmosSys.Merge(ev.Gas, outGas);
    }

    private void TakeSuffocationDamage(Entity<RespiratorComponent> ent)
    {
        if (ent.Comp.SuffocationCycles == 2)
            _adminLogger.Add(LogType.Asphyxiation, $"{ToPrettyString(ent):entity} started suffocating");

        if (ent.Comp.SuffocationCycles >= ent.Comp.SuffocationCycleThreshold)
        {
            // TODO: This is not going work with multiple different lungs, if that ever becomes a possibility
            var organs = _bodySystem.GetBodyOrganComponents<LungComponent>(ent);
            foreach (var (comp, _) in organs)
            {
                _alertsSystem.ShowAlert(ent, comp.Alert);
                RaiseLocalEvent(ent.Owner, new MoodEffectEvent("Suffocating")); // WD edit
            }
        }

        _damageableSys.TryChangeDamage(ent, ent.Comp.Damage, interruptsDoAfters: false);
    }

    private void StopSuffocation(Entity<RespiratorComponent> ent)
    {
        if (ent.Comp.SuffocationCycles >= 2)
            _adminLogger.Add(LogType.Asphyxiation, $"{ToPrettyString(ent):entity} stopped suffocating");

        // TODO: This is not going work with multiple different lungs, if that ever becomes a possibility
        var organs = _bodySystem.GetBodyOrganComponents<LungComponent>(ent);
        foreach (var (comp, _) in organs)
        {
            _alertsSystem.ClearAlert(ent, comp.Alert);
        }

        _damageableSys.TryChangeDamage(ent, ent.Comp.DamageRecovery);
    }

    public void UpdateSaturation(EntityUid uid, float amount,
        RespiratorComponent? respirator = null)
    {
        if (!Resolve(uid, ref respirator, false))
            return;

        respirator.Saturation += amount;
        respirator.Saturation =
            Math.Clamp(respirator.Saturation, respirator.MinSaturation, respirator.MaxSaturation);
    }

    private void OnApplyMetabolicMultiplier(
        Entity<RespiratorComponent> ent,
        ref ApplyMetabolicMultiplierEvent args)
    {
        if (args.Apply)
        {
            ent.Comp.UpdateInterval *= args.Multiplier;
            ent.Comp.Saturation *= args.Multiplier;
            ent.Comp.MaxSaturation *= args.Multiplier;
            ent.Comp.MinSaturation *= args.Multiplier;
            return;
        }

        // This way we don't have to worry about it breaking if the stasis bed component is destroyed
        ent.Comp.UpdateInterval /= args.Multiplier;
        ent.Comp.Saturation /= args.Multiplier;
        ent.Comp.MaxSaturation /= args.Multiplier;
        ent.Comp.MinSaturation /= args.Multiplier;
    }

    // WD start
    private void OnHandInteract(EntityUid uid, RespiratorComponent component, InteractHandEvent args)
    {
        if (!CanCPR(uid, component, args.User))
            return;

        DoCPR(uid, component, args.User);
        args.Handled = true;
    }

    private bool CanCPR(EntityUid target, RespiratorComponent comp, EntityUid user)
    {
        if (!_blocker.CanInteract(user, target))
            return false;

        if (target == user)
            return false;

        if (comp.CPRPerformedBy != null && comp.CPRPerformedBy != user)
            return false;

        if (!TryComp<HumanoidAppearanceComponent>(target, out _) && !TryComp<HumanoidAppearanceComponent>(user, out _))
            return false;

        if (!TryComp(target, out MobStateComponent? targetState))
            return false;

        if (targetState.CurrentState == MobState.Dead)
        {
            _popupSystem.PopupEntity(Loc.GetString("cpr-too-late", ("target", Identity.Entity(target, EntityManager))),
                target, user);
            return false;
        }

        if (targetState.CurrentState != MobState.Critical)
            return false;

        if (_inventorySystem.TryGetSlotEntity(user, "mask", out var maskUidUser) &&
            EntityManager.TryGetComponent<IngestionBlockerComponent>(maskUidUser, out var blockerUser) &&
            blockerUser.Enabled)
        {
            _popupSystem.PopupEntity(Loc.GetString("cpr-mask-block-user"), user, user);
            return false;
        }

        if (!_inventorySystem.TryGetSlotEntity(target, "mask", out var maskUidTarget) ||
            !EntityManager.TryGetComponent<IngestionBlockerComponent>(maskUidTarget, out var blockerTarget) ||
            !blockerTarget.Enabled)
            return true;

        _popupSystem.PopupEntity(
            Loc.GetString("cpr-mask-block-target", ("target", Identity.Entity(target, EntityManager))), target, user);
        return false;
    }

    private void DoCPR(EntityUid target, RespiratorComponent comp, EntityUid user)
    {
        var doAfterEventArgs = new DoAfterArgs(EntityManager, user, 1, new CPREndedEvent(), target, target: target)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
            BreakOnHandChange = true
        };

        if (!_doAfter.TryStartDoAfter(doAfterEventArgs))
        {
            _popupSystem.PopupEntity(Loc.GetString("cpr-failed"), user, user);
            return;
        }

        comp.CPRPerformedBy = user;

        _popupSystem.PopupEntity(Loc.GetString("cpr-started", ("target", Identity.Entity(target, EntityManager)),
                ("user", Identity.Entity(user, EntityManager))), target, PopupType.Medium);

        comp.CPRPlayingStream =
            _audio.PlayPvs(comp.CPRSound, target, AudioParams.Default.WithVolume(-3f).WithLoop(true)).Value.Entity;

        _adminLogger.Add(LogType.Action, LogImpact.High,
            $"{ToPrettyString(user):entity} начал произовдить СЛР на {ToPrettyString(target):entity}");
    }

    private void OnCPRDoAfterEnd(EntityUid uid, RespiratorComponent component, CPREndedEvent args)
    {
        if (args.Handled)
            return;

        if (args.Cancelled || !TryComp<MobStateComponent>(args.Target, out var targetState) ||
            targetState.CurrentState != MobState.Critical)
        {
            _audio.Stop(component.CPRPlayingStream);
            component.CPRPerformedBy = null;
            _popupSystem.PopupEntity(Loc.GetString("cpr-failed"), args.User, args.User);
            _adminLogger.Add(LogType.Action, LogImpact.High,
                $"{ToPrettyString(args.User):entity} не удалось произвести СЛР на {ToPrettyString(args.Target):entity}");
            return;
        }

        args.Handled = true;

        _damageable.TryChangeDamage(uid, -component.Damage * 2, true, false);

        _popupSystem.PopupEntity(Loc.GetString("cpr-cycle-ended", ("target", Identity.Entity(uid, EntityManager)),
                ("user", Identity.Entity(args.User, EntityManager))), uid);

        _adminLogger.Add(LogType.Action, LogImpact.High,
            $"{ToPrettyString(args.User):entity} произвёл СЛР на {ToPrettyString(args.Target):entity}");

        if (args.Target != null && CanCPR(args.Target.Value, component, args.User))
        {
            args.Repeat = true;
        }
        else
        {
            component.CPRPerformedBy = null;
            _audio.Stop(component.CPRPlayingStream);
        }

        RaiseLocalEvent(args.User, new MoodEffectEvent("SavedLife"));
    }
    //WD end
}

[ByRefEvent]
public record struct InhaleLocationEvent(GasMixture? Gas);

[ByRefEvent]
public record struct ExhaleLocationEvent(GasMixture? Gas);
