using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Components;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Chemistry;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using System.Threading;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.Chemistry.EntitySystems
{
    public sealed partial class ChemistrySystem
    {
        [Dependency] private readonly ReactiveSystem _reactive = default!;
        [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

        public void InitializePatch()
        {
            SubscribeLocalEvent<PatchComponent, PatchDoAfterEvent>(OnPatchDoAfter);
            SubscribeLocalEvent<PatchComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<PatchComponent, UseInHandEvent>(OnUseInHand);
            SubscribeLocalEvent<PatchComponent, SolutionContainerChangedEvent>(OnSolutionChange);

        }

        private void OnPatchDoAfter(Entity<PatchComponent> entity, ref PatchDoAfterEvent args)
        {
            if (args.Cancelled || args.Handled || args.Args.Target == null)
                return;

            TryDoInject(entity, args.Args.Target.Value, args.Args.User);
            args.Handled = true;
        }

        private void PatchDoAfter(Entity<PatchComponent> patch, EntityUid target, EntityUid user)
        {
            var (uid, component) = patch;

            // Dont need to start DoAfter if patch is empty
            if (!_solutionContainers.TryGetSolution(uid, component.SolutionName, out var _, out var patchSolution) || patchSolution.Volume == 0)
            {
                _popup.PopupCursor(Loc.GetString("patch-component-empty-message"), user);
                return;
            }

            // Create a pop-up for the user
            _popup.PopupEntity(Loc.GetString("patch-component-injecting-user"), target, user);

            var isTarget = user != target;

            if (isTarget)
            {
                // Create a pop-up for the target
                var userName = Identity.Entity(user, EntityManager);
                _popup.PopupEntity(Loc.GetString("injector-component-injecting-target",
                    ("user", userName)), user, target);
            }

            var actualDelay = MathHelper.Max(patch.Comp.Delay, TimeSpan.FromSeconds(1));

            // Injections take 0.5 seconds longer per additional 5u
            actualDelay += TimeSpan.FromSeconds(patchSolution.Volume.Float() / component.Delay.TotalSeconds - 0.5f);

            _adminLogger.Add(LogType.ForceFeed, $"{_entMan.ToPrettyString(user):user} is attempting to put a patch on {_entMan.ToPrettyString(target):target}");

            _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, user, actualDelay, new PatchDoAfterEvent(), patch.Owner, target: target, used: patch.Owner)
            {
                BreakOnUserMove = true,
                BreakOnDamage = true,
                BreakOnTargetMove = true
            });
        }

        /// <summary>
        /// Actually difference between OnUseInHand and OnAfterInteract only in target
        /// In OnUseInHand target is always = user. In OnAfterInteract target may be user or may not
        /// </summary>
        private void OnUseInHand(Entity<PatchComponent> entity, ref UseInHandEvent args)
        {
            if (args.Handled)
                return;

            if (args.User is not { Valid: true } target)
                return;

            PatchDoAfter(entity, target, args.User);

            args.Handled = true;
        }

        private void OnAfterInteract(Entity<PatchComponent> entity, ref AfterInteractEvent args)
        {
            if (!args.CanReach || args.Handled)
                return;

            var (_, component) = entity;

            if (!EligibleEntity(args.Target, _entMan, component))
                return;

            if (args.Target is not { Valid: true } target)
                return;

            var user = args.User;

            PatchDoAfter(entity, target, user);
            args.Handled = true;
        }

        private void OnSolutionChange(Entity<PatchComponent> entity, ref SolutionContainerChangedEvent args)
        {
            Dirty(entity);
        }

        private bool TryDoInject(Entity<PatchComponent> patch, EntityUid? target, EntityUid user)
        {
            var (uid, component) = patch;

            string? msgFormat = null;
            if (!EligibleEntity(target, _entMan, component))
                return false;

            if (!_solutionContainers.TryGetSolution(uid, component.SolutionName, out var patchSoln, out var patchSolution) || patchSolution.Volume == 0)
            {
                // TODO: Empty patch should stop the bleeding

                _popup.PopupCursor(Loc.GetString("patch-component-empty-message"), user);
                return true;
            }

            if (!_solutionContainers.TryGetInjectableSolution(target.Value, out var targetSoln, out var targetSolution))
            {
                _popup.PopupCursor(Loc.GetString("patch-cant-inject", ("target", Identity.Entity(target.Value, _entMan))), user);
                return false;
            }

            if (patchSolution.Volume > targetSolution.AvailableVolume)
            {
                _popup.PopupCursor(Loc.GetString("patch-cant-inject-now"), user);
                return false;
            }

            var removedSolution = _solutionContainers.SplitSolution(patchSoln.Value, patchSolution.Volume);

            _popup.PopupCursor(Loc.GetString(msgFormat ?? "patch-component-inject-other-message", ("other", target)), user);

            if (!targetSolution.CanAddSolution(removedSolution))
                return true;

            ApplyOnSkin(patch,target, targetSoln, removedSolution);
            QueueDel(patch);

            _adminLogger.Add(LogType.ForceFeed, $"{_entMan.ToPrettyString(user):user} put a patch on {_entMan.ToPrettyString(target.Value):target} with a solution {SolutionContainerSystem.ToPrettyString(removedSolution):removedSolution} using a {_entMan.ToPrettyString(uid):using}");

            return true;
        }

        /// <summary>
        /// Applies solution via ReactionMethod.Touch
        /// Applies until CancellationToken is not cancelled
        /// if CancellationToken is cancelled starts ApplyIntoBloodStream
        /// </summary>
        private void ApplyOnSkin(Entity<PatchComponent> entity, EntityUid? target, Entity<SolutionComponent>? targetSoln, Solution solution)
        {
            CancellationToken token = entity.Comp.CancelTokenSourceSkin.Token;
            Double applicationTime = (double) (entity.Comp.BaseApplicationTime + 2 * (solution.Volume / 5));
            FixedPoint2 ups = 1 / applicationTime; // ups - units per second
            var currentSolution = solution;

            currentSolution.ScaleSolution((float)ups);

            if (target.HasValue)
            {
                Timer.SpawnRepeating(TimeSpan.FromSeconds(1), () =>
                {
                    if (token.IsCancellationRequested)
                        return;
                    _reactive.DoEntityReaction(target.Value, currentSolution, ReactionMethod.Touch);
                }, token);
            }

            Timer.Spawn(TimeSpan.FromSeconds(applicationTime), () =>
            {
                        OnRemoveSkin(entity);
                        ApplyIntoBloodStream(entity, target, targetSoln, solution);
            });
        }


        /// <summary>
        /// Almost similar to the ApplyOnSkin.
        /// Applies solution into bloodstream.
        /// </summary>
        private void ApplyIntoBloodStream(Entity<PatchComponent> entity, EntityUid? target, Entity<SolutionComponent>? targetSoln, Solution solution)
        {
            CancellationToken token = entity.Comp.CancelTokenSourceBlood.Token;
            Double applicationTime = (double) (entity.Comp.BaseApplicationTime + 2 * (solution.Volume / 5));
            FixedPoint2 ups = (1 / applicationTime) / 2; // ups - units per second
            var currentSolution = solution;

            currentSolution.ScaleSolution((float)ups);

            if (targetSoln.HasValue)
            {
                Timer.SpawnRepeating(TimeSpan.FromSeconds(1), () =>
                {
                    if (token.IsCancellationRequested)
                        return;
                    _solutionContainers.TryAddSolution(targetSoln.Value, solution);

                    if (target.HasValue)
                    {
                        _reactive.DoEntityReaction(target.Value, solution, ReactionMethod.Touch);
                    }
                }, token);

                Timer.Spawn(TimeSpan.FromSeconds(applicationTime / 2), () => OnRemoveBlood(entity));
            }

        }

        // Stops Timer, otherwise ApplyOnSkin and ApplyIntoTheBloodstream will last forever
        private static void OnRemoveSkin(Entity<PatchComponent> entity)
        {
            entity.Comp.CancelTokenSourceSkin.Cancel();
        }

        private static void OnRemoveBlood(Entity<PatchComponent> entity)
        {
            entity.Comp.CancelTokenSourceBlood.Cancel();
        }

        private static bool EligibleEntity([NotNullWhen(true)] EntityUid? entity, IEntityManager entMan, PatchComponent component)
        {
            // Using patch only on mobs
            return component.OnlyMobs
                ? entMan.HasComponent<SolutionContainerManagerComponent>(entity) &&
                  entMan.HasComponent<MobStateComponent>(entity)
                : entMan.HasComponent<SolutionContainerManagerComponent>(entity);
        }
    }
}
