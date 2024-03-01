using System.Linq;
using Content.Server.Body.Components;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.Emp;
using Content.Server.EUI;
using Content.Server._White.Cult.UI;
using Content.Shared._White.Chaplain;
using Content.Shared.Chemistry.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Stacks;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared._White.Cult.Actions;
using Content.Shared.Actions;
using Content.Shared.Cuffs.Components;
using Content.Shared.DoAfter;
using Content.Shared.Mindshield.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using CultistComponent = Content.Shared._White.Cult.Components.CultistComponent;

namespace Content.Server._White.Cult.Runes.Systems;

public partial class CultSystem
{
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionSystem = default!;
    [Dependency] private readonly EmpSystem _empSystem = default!;
    [Dependency] private readonly EuiManager _euiManager = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    public void InitializeActions()
    {
        SubscribeLocalEvent<CultistComponent, CultTwistedConstructionActionEvent>(OnTwistedConstructionAction);
        SubscribeLocalEvent<CultistComponent, CultSummonDaggerActionEvent>(OnSummonDaggerAction);
        SubscribeLocalEvent<CultistComponent, CultShadowShacklesTargetActionEvent>(OnShadowShackles);
        SubscribeLocalEvent<CultistComponent, CultElectromagneticPulseInstantActionEvent>(OnElectromagneticPulse);
        SubscribeLocalEvent<CultistComponent, CultSummonCombatEquipmentTargetActionEvent>(OnSummonCombatEquipment);
        SubscribeLocalEvent<CultistComponent, CultConcealPresenceWorldActionEvent>(OnConcealPresence);
        SubscribeLocalEvent<CultistComponent, CultBloodRitesInstantActionEvent>(OnBloodRites);
        SubscribeLocalEvent<CultistComponent, CultTeleportTargetActionEvent>(OnTeleport);
        SubscribeLocalEvent<CultistComponent, CultStunTargetActionEvent>(OnStunTarget);
        SubscribeLocalEvent<CultistComponent, ActionGettingRemovedEvent>(OnActionRemoved);
        SubscribeLocalEvent<CultistComponent, ShacklesEvent>(OnShackles);
    }

    private void OnShackles(Entity<CultistComponent> ent, ref ShacklesEvent args)
    {
        if (args.Cancelled || args.Target == null)
            return;

        if (!TryComp(args.Target, out CuffableComponent? cuffable) || cuffable.Container.ContainedEntities.Count > 0)
            return;

        var cuffs = Spawn("ShadowShackles", Transform(ent).Coordinates);
        if (!_cuffable.TryAddNewCuffs(args.Target.Value, args.User, cuffs, cuffable))
            QueueDel(cuffs);
    }

    private void OnActionRemoved(Entity<CultistComponent> ent, ref ActionGettingRemovedEvent args)
    {
        ent.Comp.SelectedEmpowers.Remove(GetNetEntity(args.Action));
        Dirty(ent);
    }

    private void OnStunTarget(EntityUid uid, CultistComponent component, CultStunTargetActionEvent args)
    {
        if (args.Target == uid || !TryComp<BloodstreamComponent>(args.Performer, out var bloodstream) ||
            HasComp<HolyComponent>(args.Target) || !TryComp<StatusEffectsComponent>(args.Target, out var status))
            return;

        if (HasComp<MindShieldComponent>(args.Target))
        {
            _popupSystem.PopupEntity("Он имплантирован чипом защиты разума.", args.Performer, args.Performer);
            return;
        }

        if (!_stunSystem.TryParalyze(args.Target, TimeSpan.FromSeconds(6), true, status) &
            !_statusEffectsSystem.TryAddStatusEffect(args.Target, "Muted", TimeSpan.FromSeconds(12), true, "Muted",
                status))
            return;

        _bloodstreamSystem.TryModifyBloodLevel(uid, -10, bloodstream, createPuddle: false);
        args.Handled = true;
    }

    private void OnTeleport(EntityUid uid, CultistComponent component, CultTeleportTargetActionEvent args)
    {
        if (!TryComp<BloodstreamComponent>(args.Performer, out var bloodstream) || !TryComp<ActorComponent>(uid, out var actor))
            return;

        if (!TryComp<CultistComponent>(args.Target, out _) &&
            !(TryComp<MobStateComponent>(args.Target, out var mobStateComponent) &&
                mobStateComponent.CurrentState is not MobState.Alive))
        {
            _popupSystem.PopupEntity("Цель должна быть культистом или лежать.", args.Performer, args.Performer);
            return;
        }

        _bloodstreamSystem.TryModifyBloodLevel(uid, -5, bloodstream, createPuddle: false);

        var eui = new TeleportSpellEui(args.Performer, args.Target);
        _euiManager.OpenEui(eui, actor.PlayerSession);
        eui.StateDirty();

        args.Handled = true;
    }

    private void OnBloodRites(EntityUid uid, CultistComponent component, CultBloodRitesInstantActionEvent args)
    {
        if (!TryComp<BloodstreamComponent>(args.Performer, out var bloodstreamComponent))
            return;

        var bruteDamageGroup = _prototypeManager.Index<DamageGroupPrototype>("Brute");
        var burnDamageGroup = _prototypeManager.Index<DamageGroupPrototype>("Burn");

        var xform = Transform(uid);

        var entitiesInRange = _lookup.GetEntitiesInRange(_transform.GetMapCoordinates(xform), 1.5f);

        FixedPoint2 totalBloodAmount = 0f;

        var breakLoop = false;
        foreach (var solutionEntity in entitiesInRange.ToList())
        {
            if (breakLoop)
                break;

            if (!TryComp<PuddleComponent>(solutionEntity, out var puddleComponent))
                continue;

            if (!_solutionSystem.TryGetSolution(solutionEntity, puddleComponent.SolutionName, out var solution))
                continue;

            foreach (var solutionContent in solution.Value.Comp.Solution.Contents.ToList())
            {
                if (solutionContent.Reagent.Prototype != "Blood")
                    continue;

                totalBloodAmount += solutionContent.Quantity;

                _bloodstreamSystem.TryModifyBloodLevel(uid, solutionContent.Quantity / 6f);
                _solutionSystem.RemoveReagent((Entity<SolutionComponent>) solution, "Blood", FixedPoint2.MaxValue);

                if (GetMissingBloodValue(bloodstreamComponent) == 0)
                {
                    breakLoop = true;
                }
            }
        }

        if (totalBloodAmount == 0f)
        {
            return;
        }

        _audio.PlayPvs("/Audio/White/Cult/enter_blood.ogg", uid, AudioParams.Default);
        _damageableSystem.TryChangeDamage(uid, new DamageSpecifier(bruteDamageGroup, -20));
        _damageableSystem.TryChangeDamage(uid, new DamageSpecifier(burnDamageGroup, -20));

        args.Handled = true;
    }

    private static FixedPoint2 GetMissingBloodValue(BloodstreamComponent bloodstreamComponent)
    {
        return bloodstreamComponent.BloodMaxVolume - bloodstreamComponent.BloodSolution!.Value.Comp.Solution.Volume;
    }

    private void OnConcealPresence(EntityUid uid, CultistComponent component, CultConcealPresenceWorldActionEvent args)
    {
        if (!TryComp<BloodstreamComponent>(args.Performer, out _))
            return;
    }

    private void OnSummonCombatEquipment(
        EntityUid uid,
        CultistComponent component,
        CultSummonCombatEquipmentTargetActionEvent args)
    {
        if (!TryComp<BloodstreamComponent>(args.Performer, out var bloodstream))
            return;

        _bloodstreamSystem.TryModifyBloodLevel(uid, -20, bloodstream, createPuddle: false);

        var coordinates = Transform(uid).Coordinates;
        var helmet = Spawn("ClothingHeadHelmetCult", coordinates);
        var armor = Spawn("ClothingOuterArmorCult", coordinates);
        var shoes = Spawn("ClothingShoesCult", coordinates);
        var blade = Spawn("EldritchBlade", coordinates);
        var bola = Spawn("CultBola", coordinates);

        _inventorySystem.TryUnequip(uid, "head");
        _inventorySystem.TryUnequip(uid, "outerClothing");
        _inventorySystem.TryUnequip(uid, "shoes");

        _inventorySystem.TryEquip(uid, helmet, "head", force: true);
        _inventorySystem.TryEquip(uid, armor, "outerClothing", force: true);
        _inventorySystem.TryEquip(uid, shoes, "shoes", force: true);

        _handsSystem.PickupOrDrop(uid, blade);
        _handsSystem.PickupOrDrop(uid, bola);

        args.Handled = true;
    }

    private void OnElectromagneticPulse(
        EntityUid uid,
        CultistComponent component,
        CultElectromagneticPulseInstantActionEvent args)
    {
        if (!TryComp<BloodstreamComponent>(args.Performer, out var bloodstream))
            return;

        _bloodstreamSystem.TryModifyBloodLevel(uid, -10, bloodstream, createPuddle: false);

        _empSystem.EmpPulse(_transform.GetMapCoordinates(uid), 5, 100000, 10f);

        args.Handled = true;
    }

    private void OnShadowShackles(EntityUid uid, CultistComponent component, CultShadowShacklesTargetActionEvent args)
    {
        if (args.Target == uid || !TryComp<BloodstreamComponent>(args.Performer, out var bloodstream))
            return;

        _bloodstreamSystem.TryModifyBloodLevel(uid, -5, bloodstream, createPuddle: false);

        if (!HasComp<HolyComponent>(args.Target) &&
            _statusEffectsSystem.TryAddStatusEffect(args.Target, "Muted", TimeSpan.FromSeconds(10), true, "Muted"))
        {
            _popupSystem.PopupEntity("Цель обезмолвлена.", args.Performer, args.Performer);
            args.Handled = true;
        }

        if (!TryComp(args.Target, out CuffableComponent? cuffs) || cuffs.Container.ContainedEntities.Count > 0)
            return;

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.Performer, TimeSpan.FromSeconds(2),
            new ShacklesEvent(), args.Performer, args.Target)
        {
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            BreakOnDamage = true
        });

        args.Handled = true;
    }

    private void OnTwistedConstructionAction(
        EntityUid uid,
        CultistComponent component,
        CultTwistedConstructionActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<BloodstreamComponent>(args.Performer, out var bloodstreamComponent))
            return;

        if (!_entityManager.TryGetComponent<StackComponent>(args.Target, out var stack))
            return;

        if (stack.StackTypeId != SteelPrototypeId)
            return;

        var transform = Transform(args.Target).Coordinates;
        var count = stack.Count;

        _entityManager.DeleteEntity(args.Target);

        var material = _entityManager.SpawnEntity(RunicMetalPrototypeId, transform);

        _bloodstreamSystem.TryModifyBloodLevel(args.Performer, -stack.Count / 2f, bloodstreamComponent, false);

        if (!_entityManager.TryGetComponent<StackComponent>(material, out var stackNew))
            return;

        stackNew.Count = count;

        _popupSystem.PopupEntity("Конвертируем сталь в руинический металл!", args.Performer, args.Performer);
        args.Handled = true;
    }

    private void OnSummonDaggerAction(EntityUid uid, CultistComponent component, CultSummonDaggerActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<BloodstreamComponent>(args.Performer, out var bloodstreamComponent))
            return;

        var xform = Transform(args.Performer).Coordinates;
        var dagger = _entityManager.SpawnEntity(RitualDaggerPrototypeId, xform);

        _bloodstreamSystem.TryModifyBloodLevel(args.Performer, -20, bloodstreamComponent, false);
        _handsSystem.TryPickupAnyHand(args.Performer, dagger);
        args.Handled = true;
    }
}
