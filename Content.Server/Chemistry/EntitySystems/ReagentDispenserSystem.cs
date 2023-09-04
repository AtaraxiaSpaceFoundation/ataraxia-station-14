using Content.Server.Administration.Logs;
using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.DeviceLinking.Systems;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Dispenser;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using JetBrains.Annotations;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.FixedPoint;

namespace Content.Server.Chemistry.EntitySystems
{
    /// <summary>
    /// Contains all the server-side logic for reagent dispensers.
    /// <seealso cref="ReagentDispenserComponent"/>
    /// </summary>
    [UsedImplicitly]
    public sealed class ReagentDispenserSystem : EntitySystem
    {
        [Dependency] private readonly AudioSystem _audioSystem = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly DeviceLinkSystem _signalSystem = default!; // WD
        [Dependency] private readonly ChemMasterSystem _chemMasterSystem = default!; // WD

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ReagentDispenserComponent, ComponentStartup>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ReagentDispenserComponent, SolutionContainerChangedEvent>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ReagentDispenserComponent, EntInsertedIntoContainerMessage>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ReagentDispenserComponent, EntRemovedFromContainerMessage>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ReagentDispenserComponent, BoundUIOpenedEvent>(SubscribeUpdateUiState);

            // WD START
            SubscribeLocalEvent<ReagentDispenserComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<ReagentDispenserComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<ReagentDispenserComponent, NewLinkEvent>(OnNewLink);
            SubscribeLocalEvent<ReagentDispenserComponent, PortDisconnectedEvent>(OnPortDisconnected);
            SubscribeLocalEvent<ReagentDispenserComponent, AnchorStateChangedEvent>(OnAnchorChanged);
            // WD END

            SubscribeLocalEvent<ReagentDispenserComponent, GotEmaggedEvent>(OnEmagged);

            SubscribeLocalEvent<ReagentDispenserComponent, ReagentDispenserSetDispenseAmountMessage>(OnSetDispenseAmountMessage);
            SubscribeLocalEvent<ReagentDispenserComponent, ReagentDispenserDispenseReagentMessage>(OnDispenseReagentMessage);
            SubscribeLocalEvent<ReagentDispenserComponent, ReagentDispenserClearContainerSolutionMessage>(OnClearContainerSolutionMessage);
        }

        // WD START
        private void OnInit(EntityUid uid, ReagentDispenserComponent component, ComponentInit args)
        {
            _signalSystem.EnsureSourcePorts(uid, ReagentDispenserComponent.ChemMasterPort);
        }

        private void OnMapInit(EntityUid uid, ReagentDispenserComponent component, MapInitEvent args)
        {
            if (!TryComp<DeviceLinkSourceComponent>(uid, out var receiver))
                return;

            foreach (var port in receiver.Outputs.Values.SelectMany(ports => ports))
            {
                if (!TryComp<ChemMasterComponent>(port, out var master))
                    continue;

                UpdateConnection(uid, port, component, master);
                break;
            }
        }

        private void OnNewLink(EntityUid uid, ReagentDispenserComponent component, NewLinkEvent args)
        {
            if (TryComp<ChemMasterComponent>(args.Sink, out var master) && args.SourcePort == ReagentDispenserComponent.ChemMasterPort)
                UpdateConnection(uid, args.Sink, component, master);
        }

        private void OnPortDisconnected(EntityUid uid, ReagentDispenserComponent component, PortDisconnectedEvent args)
        {
            if (args.Port != ReagentDispenserComponent.ChemMasterPort)
                return;

            component.ChemMaster = null;
            component.ChemMasterInRange = false;
        }

        private void OnAnchorChanged(EntityUid uid, ReagentDispenserComponent component, ref AnchorStateChangedEvent args)
        {
            if (args.Anchored)
                RecheckConnections(uid, component);
        }

        public void UpdateConnection(EntityUid dispenser, EntityUid chemMaster,
            ReagentDispenserComponent? dispenserComp = null, ChemMasterComponent? chemMasterComp = null)
        {
            if (!Resolve(dispenser, ref dispenserComp) || !Resolve(chemMaster, ref chemMasterComp))
                return;

            if (dispenserComp.ChemMaster.HasValue && dispenserComp.ChemMaster.Value != chemMaster &&
                TryComp(dispenserComp.ChemMaster, out ChemMasterComponent? oldMaster))
            {
                oldMaster.ConnectedDispenser = null;
            }

            if (chemMasterComp.ConnectedDispenser.HasValue && chemMasterComp.ConnectedDispenser.Value != dispenser &&
                TryComp(dispenserComp.ChemMaster, out ReagentDispenserComponent? oldDispenser))
            {
                oldDispenser.ChemMaster = null;
                oldDispenser.ChemMasterInRange = false;
            }

            dispenserComp.ChemMaster = chemMaster;
            chemMasterComp.ConnectedDispenser = dispenser;

            RecheckConnections(dispenser, dispenserComp);
        }

        private void RecheckConnections(EntityUid dispenser, ReagentDispenserComponent? component = null)
        {
            if (!Resolve(dispenser, ref component))
                return;

            if (component.ChemMaster == null)
            {
                component.ChemMasterInRange = false;
                return;
            }

            Transform(component.ChemMaster.Value).Coordinates
                .TryDistance(EntityManager, Transform(dispenser).Coordinates, out var distance);
            component.ChemMasterInRange = distance <= 1.5f;
        }
        // WD END

        private void SubscribeUpdateUiState<T>(Entity<ReagentDispenserComponent> ent, ref T ev)
        {
            UpdateUiState(ent);
        }

        private void UpdateUiState(Entity<ReagentDispenserComponent> reagentDispenser)
        {
            var outputContainer = _itemSlotsSystem.GetItemOrNull(reagentDispenser, SharedReagentDispenser.OutputSlotName);
            var outputContainerInfo = BuildOutputContainerInfo(outputContainer);

            var inventory = GetInventory(reagentDispenser);

            var state = new ReagentDispenserBoundUserInterfaceState(outputContainerInfo, inventory, reagentDispenser.Comp.DispenseAmount);
            _userInterfaceSystem.TrySetUiState(reagentDispenser, ReagentDispenserUiKey.Key, state);
        }

        private ContainerInfo? BuildOutputContainerInfo(EntityUid? container)
        {
            if (container is not { Valid: true })
                return null;

            if (_solutionContainerSystem.TryGetFitsInDispenser(container.Value, out _, out var solution))
            {
                return new ContainerInfo(Name(container.Value), solution.Volume, solution.MaxVolume)
                {
                    Reagents = solution.Contents
                };
            }

            return null;
        }

        private List<ReagentId> GetInventory(Entity<ReagentDispenserComponent> ent)
        {
            var reagentDispenser = ent.Comp;
            var inventory = new List<ReagentId>();

            if (reagentDispenser.PackPrototypeId is not null
                && _prototypeManager.TryIndex(reagentDispenser.PackPrototypeId, out ReagentDispenserInventoryPrototype? packPrototype))
            {
                inventory.AddRange(packPrototype.Inventory.Select(x => new ReagentId(x, null)));
            }

            if (HasComp<EmaggedComponent>(ent)
                && reagentDispenser.EmagPackPrototypeId is not null
                && _prototypeManager.TryIndex(reagentDispenser.EmagPackPrototypeId, out ReagentDispenserInventoryPrototype? emagPackPrototype))
            {
                inventory.AddRange(emagPackPrototype.Inventory.Select(x => new ReagentId(x, null)));
            }

            return inventory;
        }

        private void OnEmagged(Entity<ReagentDispenserComponent> reagentDispenser, ref GotEmaggedEvent args)
        {
            // adding component manually to have correct state
            EntityManager.AddComponent<EmaggedComponent>(reagentDispenser);
            UpdateUiState(reagentDispenser);
            args.Handled = true;
        }

        private void OnSetDispenseAmountMessage(Entity<ReagentDispenserComponent> reagentDispenser, ref ReagentDispenserSetDispenseAmountMessage message)
        {
            reagentDispenser.Comp.DispenseAmount = message.ReagentDispenserDispenseAmount;
            UpdateUiState(reagentDispenser);
            ClickSound(reagentDispenser);
        }

        private void OnDispenseReagentMessage(Entity<ReagentDispenserComponent> reagentDispenser, ref ReagentDispenserDispenseReagentMessage message)
        {
            // Ensure that the reagent is something this reagent dispenser can dispense.
            if (!GetInventory(reagentDispenser).Contains(message.ReagentId))
                return;

            var outputContainer = _itemSlotsSystem.GetItemOrNull(reagentDispenser, SharedReagentDispenser.OutputSlotName);
            if (outputContainer is not { Valid: true } || !_solutionContainerSystem.TryGetFitsInDispenser(outputContainer.Value, out var solution, out _))
            { // WD EDIT START
                var chemMasterUid = reagentDispenser.Comp.ChemMaster;
                if (!reagentDispenser.Comp.ChemMasterInRange ||
                    !TryComp(chemMasterUid, out ChemMasterComponent? chemMaster) ||
                    !TryComp(chemMasterUid, out SolutionContainerManagerComponent? solutionContainer) ||
                    !_solutionContainerSystem.TryGetSolution((chemMasterUid.Value, solutionContainer),
                        SharedChemMaster.BufferSolutionName, out var bufferSolution))
                    return;

                bufferSolution.Value.Comp.Solution.AddReagent(message.ReagentId, FixedPoint2.New((int)reagentDispenser.Comp.DispenseAmount));
                _chemMasterSystem.UpdateUiState((chemMasterUid.Value, chemMaster));
                ClickSound(reagentDispenser);

                return;
            } // WD EDIT END

            if (_solutionContainerSystem.TryAddReagent(solution.Value, message.ReagentId, (int) reagentDispenser.Comp.DispenseAmount, out var dispensedAmount)
                && message.Session.AttachedEntity is not null)
            {
                _adminLogger.Add(LogType.ChemicalReaction, LogImpact.Medium,
                    $"{ToPrettyString(message.Session.AttachedEntity.Value):player} dispensed {dispensedAmount}u of {message.ReagentId} into {ToPrettyString(outputContainer.Value):entity}");
            }

            UpdateUiState(reagentDispenser);
            ClickSound(reagentDispenser);
        }

        private void OnClearContainerSolutionMessage(Entity<ReagentDispenserComponent> reagentDispenser, ref ReagentDispenserClearContainerSolutionMessage message)
        {
            var outputContainer = _itemSlotsSystem.GetItemOrNull(reagentDispenser, SharedReagentDispenser.OutputSlotName);
            if (outputContainer is not { Valid: true } || !_solutionContainerSystem.TryGetFitsInDispenser(outputContainer.Value, out var solution, out _))
                return;

            _solutionContainerSystem.RemoveAllSolution(solution.Value);
            UpdateUiState(reagentDispenser);
            ClickSound(reagentDispenser);
        }

        private void ClickSound(Entity<ReagentDispenserComponent> reagentDispenser)
        {
            _audioSystem.PlayPvs(reagentDispenser.Comp.ClickSound, reagentDispenser, AudioParams.Default.WithVolume(-2f));
        }
    }
}
