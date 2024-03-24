using System.Linq;
using System.Numerics;
using Content.Server.Body.Components;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.Emp;
using Content.Server.EUI;
using Content.Server._White.Cult.UI;
using Content.Shared._White.Chaplain;
using Content.Shared._White.Cult;
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
using Content.Shared._White.Cult.Components;
using Content.Shared._White.Cult.Systems;
using Content.Shared._White.Cult.UI;
using Content.Shared.Actions;
using Content.Shared.Cuffs.Components;
using Content.Shared.DoAfter;
using Content.Shared.Maps;
using Content.Shared.Mindshield.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
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
    [Dependency] private readonly MapSystem _mapSystem = default!;
    [Dependency] private readonly TileSystem _tile = default!;
    [Dependency] private readonly BloodSpearSystem _bloodSpear = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;

    private const string TileId = "CultFloor";
    private const string ConcealedTileId = "CultFloorConcealed";

    public void InitializeActions()
    {
        SubscribeLocalEvent<CultistComponent, CultTwistedConstructionActionEvent>(OnTwistedConstructionAction);
        SubscribeLocalEvent<CultistComponent, CultSummonDaggerActionEvent>(OnSummonDaggerAction);
        SubscribeLocalEvent<CultistComponent, CultShadowShacklesTargetActionEvent>(OnShadowShackles);
        SubscribeLocalEvent<CultistComponent, CultElectromagneticPulseInstantActionEvent>(OnElectromagneticPulse);
        SubscribeLocalEvent<CultistComponent, CultSummonCombatEquipmentTargetActionEvent>(OnSummonCombatEquipment);
        SubscribeLocalEvent<CultistComponent, CultConcealInstantActionEvent>(OnConcealPresence);
        SubscribeLocalEvent<CultistComponent, CultRevealInstantActionEvent>(OnConcealPresence);
        SubscribeLocalEvent<CultistComponent, CultBloodRitesInstantActionEvent>(OnBloodRites);
        SubscribeLocalEvent<CultistComponent, CultBloodSpearRecallInstantActionEvent>(OnBloodSpearRecall);
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
            !TryComp<StatusEffectsComponent>(args.Target, out var status))
            return;

        if (HasComp<HolyComponent>(args.Target))
        {
            _popupSystem.PopupEntity("Священная сила препятствует магии.", args.Performer, args.Performer);
            return;
        }

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
        if (!TryComp<BloodstreamComponent>(args.Performer, out var bloodstream) ||
            !TryComp<ActorComponent>(uid, out var actor))
            return;

        if (HasComp<HolyComponent>(args.Target))
        {
            _popupSystem.PopupEntity("Священная сила препятствует магии.", args.Performer, args.Performer);
            return;
        }

        if (!HasComp<CultistComponent>(args.Target) && !HasComp<ConstructComponent>(args.Target) &&
            (!TryComp<MobStateComponent>(args.Target, out var mobStateComponent) ||
             mobStateComponent.CurrentState is MobState.Alive) &&
            (!TryComp<CuffableComponent>(args.Target, out var cuffable) || cuffable.Container.Count == 0))
        {
            _popupSystem.PopupEntity("Цель должна быть культистом, быть связанной или лежать.", args.Performer,
                args.Performer);
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

                _bloodstreamSystem.TryModifyBloodLevel(uid, solutionContent.Quantity / 6f, bloodstreamComponent);
                _solutionSystem.RemoveReagent((Entity<SolutionComponent>) solution, "Blood", FixedPoint2.MaxValue);

                /*if (GetMissingBloodValue(bloodstreamComponent) == 0)
                {
                    breakLoop = true;
                }*/
            }
        }

        if (totalBloodAmount == 0f)
            return;

        component.RitesBloodAmount += totalBloodAmount;

        _audio.PlayPvs("/Audio/White/Cult/enter_blood.ogg", uid, AudioParams.Default);
        _damageableSystem.TryChangeDamage(uid, new DamageSpecifier(bruteDamageGroup, -20));
        _damageableSystem.TryChangeDamage(uid, new DamageSpecifier(burnDamageGroup, -20));

        _popupSystem.PopupEntity(Loc.GetString("verb-blood-rites-message", ("blood", component.RitesBloodAmount)), uid,
            uid);

        args.Handled = true;
    }

    private static FixedPoint2 GetMissingBloodValue(BloodstreamComponent bloodstreamComponent)
    {
        return bloodstreamComponent.BloodMaxVolume - bloodstreamComponent.BloodSolution!.Value.Comp.Solution.Volume;
    }

    private void OnBloodSpearRecall(Entity<CultistComponent> ent, ref CultBloodSpearRecallInstantActionEvent args)
    {
        if (ent.Comp.BloodSpear == null)
        {
            _bloodSpear.DetachSpearFromUser(ent);
            return;
        }

        var spear = ent.Comp.BloodSpear.Value;
        var xform = Transform(spear);
        var coords = _transform.GetWorldPosition(xform);
        var userCoords = _transform.GetWorldPosition(ent);
        var distance = (userCoords - coords).Length();
        if (distance > 10f)
        {
            _popupSystem.PopupEntity("Копьё слишком далеко!", ent, ent);
            return;
        }

        TryComp<PhysicsComponent>(spear, out var physics);
        _physics.SetBodyType(spear, BodyType.Dynamic, body: physics, xform: xform);
        _transform.AttachToGridOrMap(spear, xform);
        _transform.SetWorldPosition(xform, userCoords);
        _handsSystem.TryPickupAnyHand(ent, spear, animate: false);

        args.Handled = true;
    }

    private void OnConcealPresence(EntityUid uid, CultistComponent component, CultConcealPresenceInstantActionEvent args)
    {
        if (!TryComp<BloodstreamComponent>(args.Performer, out var bloodstream) ||
            !TryComp<TransformComponent>(args.Performer, out var xform))
            return;

        var conceal = args is CultConcealInstantActionEvent;

        var concealableQuery = GetEntityQuery<ConcealableComponent>();
        var appearanceQuery = GetEntityQuery<AppearanceComponent>();

        const float radius = 5f;

        var entitiesInRange = _lookup.GetEntitiesInRange(_transform.GetMapCoordinates(xform), radius);

        var success = false;

        // Conceal/Reveal runes and structures
        foreach (var ent in entitiesInRange)
        {
            if (!concealableQuery.TryGetComponent(ent, out var concealable) ||
                !appearanceQuery.TryGetComponent(ent, out var appearance) ||
                !EntityManager.MetaQuery.TryGetComponent(ent, out var meta))
                continue;

            if (concealable.Concealed == conceal)
                continue;

            _appearanceSystem.SetData(ent, ConcealableAppearance.Concealed, conceal, appearance);

            concealable.Concealed = conceal;

            RaiseLocalEvent(ent, new ConcealEvent(conceal));

            if (concealable.ChangeMeta)
            {
                if (conceal)
                {
                    _metaDataSystem.SetEntityName(ent, concealable.ConcealedName, meta);
                    _metaDataSystem.SetEntityDescription(ent, concealable.ConcealedDesc, meta);
                }
                else
                {
                    _metaDataSystem.SetEntityName(ent, concealable.RevealedName, meta);
                    _metaDataSystem.SetEntityDescription(ent, concealable.RevealedDesc, meta);
                }
            }

            Dirty(ent, concealable, meta);

            success = true;
        }

        var gridUid = xform.GridUid;
        var pos = xform.Coordinates;

        var cultTileDef = (ContentTileDefinition) _tileDefinition[TileId];
        var concealedTileDef = (ContentTileDefinition) _tileDefinition[ConcealedTileId];

        // Conceal/Reveal tiles
        if (TryComp(gridUid, out MapGridComponent? mapGrid))
        {
            var tileRefs = _mapSystem.GetLocalTilesIntersecting(gridUid.Value, mapGrid,
                new Box2(pos.Position + new Vector2(-radius, -radius), pos.Position + new Vector2(radius, radius)));

            foreach (var tile in tileRefs)
            {
                var tilePos = _turf.GetTileCenter(tile);

                if (!pos.InRange(EntityManager, _transform, tilePos, radius))
                    continue;

                if (conceal)
                {
                    if (tile.Tile.TypeId != cultTileDef.TileId)
                        continue;

                    _tile.ReplaceTile(tile, concealedTileDef);
                    success = true;
                }
                else
                {
                    if (tile.Tile.TypeId != concealedTileDef.TileId)
                        continue;

                    _tile.ReplaceTile(tile, cultTileDef);
                    success = true;
                }
            }
        }

        if (success)
        {
            _audio.PlayPvs(conceal ? "/Audio/White/Cult/smoke.ogg" : "/Audio/White/Cult/enter_blood.ogg", uid,
                AudioParams.Default.WithMaxDistance(2f));
            _bloodstreamSystem.TryModifyBloodLevel(uid, -2, bloodstream, createPuddle: false);
            args.Handled = true;
        }

        var spellQuery = GetEntityQuery<ConcealPresenceSpellComponent>();
        var actionQuery = GetEntityQuery<InstantActionComponent>();

        // Alter spell concealing/revealing state
        foreach (var empower in component.SelectedEmpowers)
        {
            if (empower == null)
                continue;

            var ent = GetEntity(empower.Value);

            if (!spellQuery.TryGetComponent(ent, out var spell) ||
                !actionQuery.TryGetComponent(ent, out var action))
                continue;

            if (conceal)
            {
                spell.Revealing = true;
                action.Icon = spell.RevealIcon;
                action.Event = spell.RevealEvent;
            }
            else
            {
                spell.Revealing = false;
                action.Icon = spell.ConcealIcon;
                action.Event = spell.ConcealEvent;
            }

            Dirty(ent, action);
        }
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

        _bloodstreamSystem.TryModifyBloodLevel(args.Performer, -10, bloodstreamComponent, false);
        _handsSystem.TryPickupAnyHand(args.Performer, dagger);
        args.Handled = true;
    }
}
