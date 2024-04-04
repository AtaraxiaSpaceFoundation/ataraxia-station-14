using System.Linq;
using Content.Server._White.Cult.GameRule;
using Content.Server._White.Mood;
using Content.Server._White.Other.FastAndFuriousSystem;
using Content.Server.Administration.Systems;
using Content.Server.Bible.Components;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Borer;
using Content.Server.Carrying;
using Content.Server.Cuffs;
using Content.Server.DoAfter;
using Content.Server.Emp;
using Content.Server.Flash.Components;
using Content.Server.Forensics;
using Content.Server.GameTicking.Rules;
using Content.Server.Humanoid;
using Content.Server.IdentityManagement;
using Content.Server.Mind;
using Content.Server.NPC.Components;
using Content.Server.NPC.Systems;
using Content.Server.Polymorph.Systems;
using Content.Server.Popups;
using Content.Server.Store.Components;
using Content.Server.Temperature.Components;
using Content.Server.Temperature.Systems;
using Content.Shared._White.Chaplain;
using Content.Shared._White.Overlays;
using Content.Shared.Actions;
using Content.Shared.Borer;
using Content.Shared.Changeling;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Cuffs.Components;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Implants.Components;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Miracle.UI;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Ninja.Components;
using Content.Shared.Pulling;
using Content.Shared.Pulling.Components;
using Content.Shared.Standing;
using Content.Shared.StatusEffect;
using Content.Shared.Tag;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager;

namespace Content.Server.Changeling;

public sealed partial class ChangelingSystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly MobThresholdSystem _mobThresholdSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly StandingStateSystem _stateSystem = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoidAppearance = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly IdentitySystem _identity = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly ISerializationManager _serializationManager = default!;
    [Dependency] private readonly RejuvenateSystem _rejuvenate = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;
    [Dependency] private readonly TemperatureSystem _temperatureSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainerSystem = default!;
    [Dependency] private readonly SharedPullingSystem _pullingSystem = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly BloodstreamSystem _blood = default!;
    [Dependency] private readonly CuffableSystem _cuffable = default!;
    [Dependency] private readonly NukeopsRuleSystem _nukeOps = default!;
    [Dependency] private readonly CultRuleSystem _cult = default!;
    [Dependency] private readonly RevolutionaryRuleSystem _rev = default!;
    [Dependency] private readonly NpcFactionSystem _faction = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly EmpSystem _empSystem = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly ServerBorerSystem _borer = default!;
    [Dependency] private readonly CarryingSystem _carrying = default!;
    [Dependency] private readonly MoodSystem _mood = default!;

    private void InitializeAbilities()
    {
        SubscribeLocalEvent<ChangelingComponent, AbsorbDnaActionEvent>(OnAbsorb);
        SubscribeLocalEvent<ChangelingComponent, TransformActionEvent>(OnTransform);
        SubscribeLocalEvent<ChangelingComponent, RegenerateActionEvent>(OnRegenerate);
        SubscribeLocalEvent<ChangelingComponent, LesserFormActionEvent>(OnLesserForm);

        SubscribeLocalEvent<ChangelingComponent, ExtractionStingActionEvent>(OnExtractionSting);
        SubscribeLocalEvent<ChangelingComponent, TransformStingActionEvent>(OnTransformSting);
        SubscribeLocalEvent<ChangelingComponent, TransformStingItemSelectedMessage>(OnTransformStingMessage);
        SubscribeLocalEvent<ChangelingComponent, BlindStingActionEvent>(OnBlindSting);
        SubscribeLocalEvent<ChangelingComponent, MuteStingActionEvent>(OnMuteSting);
        SubscribeLocalEvent<ChangelingComponent, HallucinationStingActionEvent>(OnHallucinationSting);
        SubscribeLocalEvent<ChangelingComponent, CryoStingActionEvent>(OnCryoSting);

        SubscribeLocalEvent<ChangelingComponent, AdrenalineSacsActionEvent>(OnAdrenalineSacs);
        SubscribeLocalEvent<ChangelingComponent, FleshmendActionEvent>(OnFleshMend);
        SubscribeLocalEvent<ChangelingComponent, BiodegradeActionEvent>(OnBiodegrade);
        SubscribeLocalEvent<ChangelingComponent, AugmentedEyesightActionEvent>(OnEyesight);
        SubscribeLocalEvent<ChangelingComponent, DissonantShriekActionEvent>(OnDissonantShriek);

        SubscribeLocalEvent<ChangelingComponent, ArmbladeActionEvent>(OnArmBlade);
        SubscribeLocalEvent<ChangelingComponent, OrganicShieldActionEvent>(OnShield);
        SubscribeLocalEvent<ChangelingComponent, ChitinousArmorActionEvent>(OnArmor);
        SubscribeLocalEvent<ChangelingComponent, HiveHeadActionEvent>(OnHiveHead);
        SubscribeLocalEvent<ChangelingComponent, TentacleArmActionEvent>(OnTentacleArm);

        SubscribeLocalEvent<ChangelingComponent, TransformDoAfterEvent>(OnTransformDoAfter);
        SubscribeLocalEvent<ChangelingComponent, AbsorbDnaDoAfterEvent>(OnAbsorbDoAfter);
        SubscribeLocalEvent<ChangelingComponent, RegenerateDoAfterEvent>(OnRegenerateDoAfter);
        SubscribeLocalEvent<ChangelingComponent, LesserFormDoAfterEvent>(OnLesserFormDoAfter);

        SubscribeLocalEvent<ChangelingComponent, ListViewItemSelectedMessage>(OnTransformUiMessage);

        SubscribeLocalEvent<ChangelingComponent, AugmentedEyesightPurchasedEvent>(OnEyesightPurchased);
        SubscribeLocalEvent<ChangelingComponent, VoidAdaptationPurchasedEvent>(OnVoidAdaptationPurchased);
    }

#region Data

    private const string ChangelingAbsorb = "ActionChangelingAbsorb";
    private const string ChangelingTransform = "ActionChangelingTransform";
    private const string ChangelingRegenerate = "ActionChangelingRegenerate";
    private const string ChangelingLesserForm = "ActionChangelingLesserForm";
    private const string ChangelingTransformSting = "ActionTransformSting";
    private const string ChangelingBlindSting = "ActionBlindSting";
    private const string ChangelingMuteSting = "ActionMuteSting";
    private const string ChangelingHallucinationSting = "ActionHallucinationSting";
    private const string ChangelingCryoSting = "ActionCryoSting";
    private const string ChangelingAdrenalineSacs = "ActionAdrenalineSacs";
    private const string ChangelingFleshMend = "ActionFleshmend";
    private const string ChangelingArmBlade = "ActionArmblade";
    private const string ChangelingShield = "ActionShield";
    private const string ChangelingArmor = "ActionArmor";
    private const string ChangelingTentacleArm = "ActionTentacleArm";

    private const string OuterName = "outerClothing";
    private const string HeadName = "head";

    private const string ArmorName = "ClothingOuterChangeling";
    private const string HelmetName = "ClothingHeadHelmetLing";
    private const string HiveHeadName = "ClothingHeadHelmetLingHive";

#endregion

#region Handlers

    private void OnAbsorb(EntityUid uid, ChangelingComponent component, AbsorbDnaActionEvent args)
    {
        if (!HasComp<HumanoidAppearanceComponent>(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("changeling-popup-absorb-not-human"), args.Performer, args.Performer);
            return;
        }

        if (HasComp<AbsorbedComponent>(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("changeling-popup-already-absorbed"), args.Performer, args.Performer);
            return;
        }

        if (!TryComp<DnaComponent>(args.Target, out _) ||
            _tag.HasTag(args.Target, "Unimplantable")) // Terminator check
        {
            _popup.PopupEntity(Loc.GetString("changeling-popup-absorb-unknown"), uid, uid);
            return;
        }

        if (!_stateSystem.IsDown(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("changeling-popup-absorb-down"), args.Performer, args.Performer);
            return;
        }

        if (!TryComp<SharedPullableComponent>(args.Target, out var pulled))
        {
            _popup.PopupEntity(Loc.GetString("changeling-popup-absorb-pull"), args.Performer, args.Performer);
            return;
        }

        if (!pulled.BeingPulled)
        {
            _popup.PopupEntity(Loc.GetString("changeling-popup-absorb-pull"), args.Performer, args.Performer);
            return;
        }

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.Performer, component.AbsorbDnaDelay,
                new AbsorbDnaDoAfterEvent(), uid, args.Target, uid)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true
            }
        );
    }

    private void OnTransform(EntityUid uid, ChangelingComponent component, TransformActionEvent args)
    {
        if (!TryComp<ActorComponent>(uid, out var actorComponent))
            return;

        if (component.AbsorbedEntities.Count <= 1 && !component.IsLesserForm)
        {
            _popup.PopupEntity(Loc.GetString("changeling-popup-transform-no-dna"), uid, uid);
            return;
        }

        if (!_ui.TryGetUi(uid, ListViewSelectorUiKeyChangeling.Key, out var bui))
            return;

        Dictionary<string, string> state;

        if (TryComp<DnaComponent>(uid, out var dnaComponent))
        {
            state = component.AbsorbedEntities.Where(key => key.Key != dnaComponent.DNA).ToDictionary(humanoidData
                => humanoidData.Key, humanoidData
                => humanoidData.Value.Name);
        }
        else
        {
            state = component.AbsorbedEntities.ToDictionary(humanoidData
                => humanoidData.Key, humanoidData
                => humanoidData.Value.Name);
        }

        _ui.SetUiState(bui, new ListViewBuiState(state));
        _ui.OpenUi(bui, actorComponent.PlayerSession);
    }

    private void OnTransformUiMessage(EntityUid uid, ChangelingComponent component, ListViewItemSelectedMessage args)
    {
        var selectedDna = args.SelectedItem;
        var user = GetEntity(args.Entity);

        _doAfterSystem.TryStartDoAfter(
            new DoAfterArgs(EntityManager, user, component.TransformDelay,
                new TransformDoAfterEvent { SelectedDna = selectedDna }, user, user, user)
            {
                BreakOnUserMove = true
            }
        );

        if (!TryComp<ActorComponent>(uid, out var actorComponent))
            return;

        if (!_ui.TryGetUi(user, ListViewSelectorUiKeyChangeling.Key, out var bui))
            return;

        _ui.CloseUi(bui, actorComponent.PlayerSession);
    }

    private void OnRegenerate(EntityUid uid, ChangelingComponent component, RegenerateActionEvent args)
    {
        if (!TryComp<DamageableComponent>(uid, out var damageableComponent))
            return;

        if (component.ChemicalsBalance < 15)
        {
            _popup.PopupEntity(Loc.GetString("changeling-popup-lack-chemicals"), uid, uid);
            return;
        }

        if (damageableComponent.TotalDamage >= 0 && !_mobStateSystem.IsDead(uid))
        {
            KillUser(uid, "Cellular");
        }

        _popup.PopupEntity(Loc.GetString("changeling-popup-start-regeneration"), uid, uid);

        _doAfterSystem.TryStartDoAfter(
            new DoAfterArgs(EntityManager, args.Performer, component.RegenerateDelay,
                new RegenerateDoAfterEvent(), args.Performer,
                args.Performer, args.Performer)
            {
                RequireCanInteract = false,
                Hidden = true
            });

        component.IsRegenerating = true;
    }

    private void OnLesserForm(EntityUid uid, ChangelingComponent component, LesserFormActionEvent args)
    {
        if (!_mobStateSystem.IsAlive(uid))
        {
            _popup.PopupEntity(Loc.GetString("changeling-popup-cant-perform"), uid, uid);
            return;
        }

        if (component.IsLesserForm)
        {
            _popup.PopupEntity(Loc.GetString("changeling-popup-already-lesser-form"), uid, uid);
            return;
        }

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.Performer, component.LesserFormDelay,
            new LesserFormDoAfterEvent(), args.Performer, args.Performer)
        {
            BreakOnUserMove = true,
            RequireCanInteract = false
        });
    }

    private void OnExtractionSting(EntityUid uid, ChangelingComponent component, ExtractionStingActionEvent args)
    {
        if (!HasComp<HumanoidAppearanceComponent>(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("changeling-popup-absorb-not-human"), args.Performer, args.Performer);
            return;
        }

        if (HasComp<AbsorbedComponent>(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("changeling-popup-already-absorbed"), args.Performer, args.Performer);
            return;
        }

        if (!TryComp<DnaComponent>(args.Target, out var dnaComponent) ||
            _tag.HasTag(args.Target, "Unimplantable")) // Terminator check
        {
            _popup.PopupEntity(Loc.GetString("changeling-popup-absorb-unknown"), uid, uid);
            return;
        }

        if (component.AbsorbedEntities.ContainsKey(dnaComponent.DNA))
        {
            _popup.PopupEntity(Loc.GetString("changeling-popup-already-absorbed"), uid, uid);
            return;
        }

        if (!TakeChemicals(uid, component, 25))
            return;

        _popup.PopupEntity(Loc.GetString("changeling-popup-dna-taken"), uid, uid);
        CopyHumanoidData(uid, args.Target, component);
        args.Handled = true;
    }

    private void OnTransformSting(EntityUid uid, ChangelingComponent component, TransformStingActionEvent args)
    {
        if (!HasComp<HumanoidAppearanceComponent>(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("changeling-popup-cant-transform-someone"), args.Performer,
                args.Performer);

            return;
        }

        if (!TryComp<ActorComponent>(uid, out var actorComponent))
            return;

        if (component.AbsorbedEntities.Count < 1)
        {
            _popup.PopupEntity(Loc.GetString("changeling-popup-transform-no-dna"), uid, uid);
            return;
        }

        if (!_ui.TryGetUi(uid, TransformStingSelectorUiKey.Key, out var bui))
            return;

        var target = GetNetEntity(args.Target);

        var state = component.AbsorbedEntities.ToDictionary(humanoidData
            => humanoidData.Key, humanoidData
            => humanoidData.Value.Name);

        _ui.SetUiState(bui, new TransformStingBuiState(state, target));
        _ui.OpenUi(bui, actorComponent.PlayerSession);
    }

    private void OnTransformStingMessage(
        EntityUid uid,
        ChangelingComponent component,
        TransformStingItemSelectedMessage args)
    {
        var selectedDna = args.SelectedItem;
        var humanData = component.AbsorbedEntities[selectedDna];
        var target = GetEntity(args.Target);
        var user = GetEntity(args.Entity);

        if (!Transform(user).Coordinates.InRange(EntityManager, _transform, Transform(target).Coordinates,
                SharedInteractionSystem.InteractionRange))
        {
            _popup.PopupEntity(Loc.GetString("changeling-popup-transform-too-far"), user, user);
            return;
        }

        if (!TryComp<ActorComponent>(uid, out var actorComponent))
            return;

        if (!_ui.TryGetUi(user, TransformStingSelectorUiKey.Key, out var bui))
            return;

        if (HasComp<ChangelingComponent>(target) || HasComp<SpaceNinjaComponent>(target) ||
            _tag.HasTag(target, "Unimplantable")) // Terminator check
        {
            _popup.PopupEntity(Loc.GetString("changeling-popup-transform-not-effective"), user, user);
            return;
        }

        if (!TakeChemicals(uid, component, 50))
            return;

        if (TryComp(target, out SharedPullerComponent? puller) && puller.Pulling is { } pulled &&
            TryComp(pulled, out SharedPullableComponent? pullable))
        {
            _pullingSystem.TryStopPull(pullable);
        }

        var oldData = CompOrNull<TransformStungComponent>(target)?.OriginalHumanoidData;

        var transformed = TransformPerson(target, humanData);

        if (transformed != null)
        {
            oldData ??= GetHumanoidData(target);
            if (oldData != null)
            {
                var transformStung = EnsureComp<TransformStungComponent>(transformed.Value);
                transformStung.OriginalHumanoidData = oldData.Value;
            }
        }

        _ui.CloseUi(bui, actorComponent.PlayerSession);

        StartUseDelayById(uid, ChangelingTransformSting);
    }

    private void OnBlindSting(EntityUid uid, ChangelingComponent component, BlindStingActionEvent args)
    {
        if (!HasComp<HumanoidAppearanceComponent>(args.Target) ||
            !HasComp<BlindableComponent>(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("changeling-popup-cant-sting"), uid, uid);
            return;
        }

        if (!TakeChemicals(uid, component, 25))
            return;

        var statusTimeSpan = TimeSpan.FromSeconds(25);
        _statusEffectsSystem.TryAddStatusEffect(args.Target, TemporaryBlindnessSystem.BlindingStatusEffect,
            statusTimeSpan, false, TemporaryBlindnessSystem.BlindingStatusEffect);

        args.Handled = true;
    }

    private void OnMuteSting(EntityUid uid, ChangelingComponent component, MuteStingActionEvent args)
    {
        if (!HasComp<HumanoidAppearanceComponent>(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("changeling-popup-cant-sting"), uid, uid);
            return;
        }

        if (!TakeChemicals(uid, component, 20))
            return;

        var statusTimeSpan = TimeSpan.FromSeconds(30);
        _statusEffectsSystem.TryAddStatusEffect(args.Target, "Muted", statusTimeSpan, false, "Muted");

        args.Handled = true;
    }

    private void OnHallucinationSting(EntityUid uid, ChangelingComponent component, HallucinationStingActionEvent args)
    {
        if (!HasComp<HumanoidAppearanceComponent>(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("changeling-popup-cant-sting"), uid, uid);
            return;
        }

        if (!TakeChemicals(uid, component, 5))
            return;

        var statusTimeSpan = TimeSpan.FromSeconds(30);
        _statusEffectsSystem.TryAddStatusEffect(args.Target, "BlurryVision", statusTimeSpan, false, "BlurryVision");

        args.Handled = true;
    }

    private void OnCryoSting(EntityUid uid, ChangelingComponent component, CryoStingActionEvent args)
    {
        if (!HasComp<HumanoidAppearanceComponent>(args.Target) ||
            !TryComp(args.Target, out TemperatureComponent? temperature))
        {
            _popup.PopupEntity(Loc.GetString("changeling-popup-cant-sting"), uid, uid);
            return;
        }

        if (!TakeChemicals(uid, component, 15))
            return;

        _temperatureSystem.ForceChangeTemperature(args.Target, MathF.Min(70, temperature.CurrentTemperature),
            temperature);

        args.Handled = true;
    }

    private void OnAdrenalineSacs(EntityUid uid, ChangelingComponent component, AdrenalineSacsActionEvent args)
    {
        if (_mobStateSystem.IsDead(uid))
            return;

        if (!_solutionContainer.TryGetInjectableSolution(uid, out var injectable, out _))
            return;

        if (!TakeChemicals(uid, component, 30))
            return;

        _solutionContainer.TryAddReagent(injectable.Value, "Stimulants", 5);

        args.Handled = true;
    }

    private void OnFleshMend(EntityUid uid, ChangelingComponent component, FleshmendActionEvent args)
    {
        if (_mobStateSystem.IsDead(uid))
            return;

        if (!_solutionContainer.TryGetInjectableSolution(uid, out var injectable, out _))
            return;

        if (!TakeChemicals(uid, component, 20))
            return;

        _solutionContainer.TryAddReagent(injectable.Value, "Ichor", 10);
        if (TryComp(uid, out BloodstreamComponent? bloodstream))
        {
            _blood.TryModifyBleedAmount(uid, -bloodstream.BleedAmount, bloodstream);
        }

        args.Handled = true;
    }

    private void OnBiodegrade(EntityUid uid, ChangelingComponent component, BiodegradeActionEvent args)
    {
        if (!_mobStateSystem.IsAlive(uid))
            return;

        if (!TryComp(uid, out CuffableComponent? cuffs) || cuffs.Container.ContainedEntities.Count < 1)
            return;

        if (!TakeChemicals(uid, component, 30))
            return;

        var lastAddedCuffs = cuffs.LastAddedCuffs;

        _cuffable.Uncuff(uid, null, lastAddedCuffs);

        Del(lastAddedCuffs);

        _popup.PopupEntity(Loc.GetString("changeling-popup-biodegrade"), uid);

        args.Handled = true;
    }

    private void OnArmBlade(EntityUid uid, ChangelingComponent component, ArmbladeActionEvent args)
    {
        SpawnOrDeleteItem(uid, component, "ArmBlade", 20);

        args.Handled = true;
    }

    private void OnShield(EntityUid uid, ChangelingComponent component, OrganicShieldActionEvent args)
    {
        SpawnOrDeleteItem(uid, component, "OrganicShield", 20);

        args.Handled = true;
    }

    private void OnArmor(EntityUid uid, ChangelingComponent component, ChitinousArmorActionEvent args)
    {
        _inventorySystem.TryUnequip(uid, OuterName, out var outer, true, true);
        _inventorySystem.TryUnequip(uid, HeadName, out var helmet, true, true);

        if (TryComp(outer, out MetaDataComponent? metaData) && metaData.EntityPrototype is {ID: ArmorName})
        {
            args.Handled = true;
            return;
        }

        if (!TakeChemicals(uid, component, 20))
        {
            if (outer != null)
                _inventorySystem.TryEquip(uid, outer.Value, OuterName, true, true);

            if (helmet != null)
                _inventorySystem.TryEquip(uid, helmet.Value, HeadName, true, true);

            return;
        }

        _inventorySystem.SpawnItemInSlot(uid, OuterName, ArmorName, true, true);
        _inventorySystem.SpawnItemInSlot(uid, HeadName, HelmetName, true, true);

        args.Handled = true;
    }

    private void OnHiveHead(EntityUid uid, ChangelingComponent component, HiveHeadActionEvent args)
    {
        if (!_mobStateSystem.IsAlive(uid))
            return;

        _inventorySystem.TryUnequip(uid, HeadName, out var helmet, true, true);

        if (TryComp(helmet, out MetaDataComponent? metaData) && metaData.EntityPrototype != null)
        {
            switch (metaData.EntityPrototype.ID)
            {
                case HiveHeadName:
                    args.Handled = true;
                    return;
                case HelmetName:
                    _inventorySystem.TryUnequip(uid, OuterName, out _, true, true);
                    break;
            }
        }

        if (!TakeChemicals(uid, component, 15))
        {
            if (helmet != null)
                _inventorySystem.TryEquip(uid, helmet.Value, HeadName, true, true);

            return;
        }

        _inventorySystem.SpawnItemInSlot(uid, HeadName, HiveHeadName, true, true);

        args.Handled = true;
    }

    private void OnTentacleArm(EntityUid uid, ChangelingComponent component, TentacleArmActionEvent args)
    {
        SpawnOrDeleteItem(uid, component, "TentacleArmGun", 10);

        args.Handled = true;
    }

    private void OnEyesightPurchased(Entity<ChangelingComponent> ent, ref AugmentedEyesightPurchasedEvent args)
    {
        EnsureComp<FlashImmunityComponent>(ent);
        EnsureComp<EyeProtectionComponent>(ent);
    }

    private void OnVoidAdaptationPurchased(Entity<ChangelingComponent> ent, ref VoidAdaptationPurchasedEvent args)
    {
        EnsureComp<VoidAdaptationComponent>(ent);
    }

    private void OnEyesight(Entity<ChangelingComponent> ent, ref AugmentedEyesightActionEvent args)
    {
        if (!_mobStateSystem.IsAlive(ent))
            return;

        args.Handled = true;

        if (HasComp<TemporaryNightVisionComponent>(ent))
        {
            RemComp<TemporaryNightVisionComponent>(ent);
            EnsureComp<FlashImmunityComponent>(ent);
            EnsureComp<EyeProtectionComponent>(ent);
            return;
        }

        EnsureComp<TemporaryNightVisionComponent>(ent);
        RemComp<FlashImmunityComponent>(ent);
        RemComp<EyeProtectionComponent>(ent);
    }

    private void OnDissonantShriek(Entity<ChangelingComponent> ent, ref DissonantShriekActionEvent args)
    {
        if (!_mobStateSystem.IsAlive(ent))
            return;

        if (!TakeChemicals(ent, ent.Comp, 20))
            return;

        _empSystem.EmpPulse(_transform.GetMapCoordinates(ent), 5, 100000, 10f);

        args.Handled = true;
    }

#endregion

#region DoAfters

    private void OnTransformDoAfter(EntityUid uid, ChangelingComponent component, TransformDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (!TakeChemicals(uid, component, 5))
            return;

        if (TryComp(uid, out SharedPullerComponent? puller) && puller.Pulling is { } pulled &&
            TryComp(pulled, out SharedPullableComponent? pullable))
            _pullingSystem.TryStopPull(pullable);

        TryTransformChangeling(args.User, args.SelectedDna, component);

        args.Handled = true;
    }

    private void OnAbsorbDoAfter(EntityUid uid, ChangelingComponent component, AbsorbDnaDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target == null)
        {
            return;
        }

        if (!_mindSystem.TryGetMind(uid, out var mindId, out _))
            return;

        component.AbsorbedCount++;

        _chemicalsSystem.AddChemicals(uid, component, 50);

        if (_container.TryGetContainer(uid, ImplanterComponent.ImplantSlotId, out var implantContainer))
        {
            foreach (var implant in implantContainer.ContainedEntities)
            {
                if (!TryComp<StoreComponent>(implant, out var store) || store.Preset != "StorePresetChangeling")
                    continue;

                store.Refunds = true;
                store.RefundAllowed = true;
            }
        }

        if (TryComp(uid, out SharedPullerComponent? puller) && puller.Pulling is { } pulled &&
            TryComp(pulled, out SharedPullableComponent? pullable))
        {
            _pullingSystem.TryStopPull(pullable);
        }

        if (TryComp<ChangelingComponent>(args.Target.Value, out var changelingComponent))
        {
            var total = component.AbsorbedEntities
                .Concat(changelingComponent.AbsorbedEntities)
                .ToDictionary(pair => pair.Key, pair => pair.Value);

            component.AbsorbedEntities = total;
        }
        else
        {
            CopyHumanoidData(uid, args.Target.Value, component);
        }

        if (TryComp<ChangelingComponent>(args.Target.Value, out _))
        {
            AbsorbLing(uid, component);
        }

        KillUser(args.Target.Value, "Cellular");

        EnsureComp<AbsorbedComponent>(args.Target.Value, out var absorbedComponent);

        absorbedComponent.AbsorberMind = mindId;

        EnsureComp<UncloneableComponent>(args.Target.Value);

        StartUseDelayById(uid, ChangelingAbsorb);

        args.Handled = true;
    }

    private void OnRegenerateDoAfter(EntityUid uid, ChangelingComponent component, RegenerateDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target == null)
        {
            component.IsRegenerating = false;
            return;
        }

        if (HasComp<AbsorbedComponent>(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("changeling-popup-was-absorbed"), args.Target.Value, args.Target.Value);
            component.IsRegenerating = false;
            return;
        }

        if (!TakeChemicals(uid, component, 15))
        {
            component.IsRegenerating = false;
            return;
        }

        _rejuvenate.PerformRejuvenate(args.Target.Value);

        _popup.PopupEntity(Loc.GetString("changeling-popup-fully-regenerated"), args.Target.Value, args.Target.Value);

        component.IsRegenerating = false;

        StartUseDelayById(uid, ChangelingRegenerate);

        args.Handled = true;
    }

    private void OnLesserFormDoAfter(EntityUid uid, ChangelingComponent component, LesserFormDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (!TakeChemicals(uid, component, 5))
            return;

        BeforeTransform(args.User);

        var polymorphEntity = _polymorph.PolymorphEntity(args.User, "MonkeyChangeling");

        if (polymorphEntity == null)
            return;

        var toAdd = new ChangelingComponent
        {
            HiveName = component.HiveName,
            ChemicalCapacity = component.ChemicalCapacity,
            ChemicalsBalance = component.ChemicalsBalance,
            AbsorbedEntities = component.AbsorbedEntities,
            IsInited = component.IsInited,
            AbsorbedCount = component.AbsorbedCount,
            IsLesserForm = true
        };

        EntityManager.AddComponent(polymorphEntity.Value, toAdd);

        TransferComponents(uid, polymorphEntity.Value);

        _implantSystem.TransferImplants(uid, polymorphEntity.Value);
        _actionContainerSystem.TransferAllActionsFiltered(uid, polymorphEntity.Value, polymorphEntity.Value);
        _action.GrantContainedActions(polymorphEntity.Value, polymorphEntity.Value);

        RemoveLesserFormActions(polymorphEntity.Value);

        _chemicalsSystem.UpdateAlert(polymorphEntity.Value, component);

        args.Handled = true;
    }

#endregion

#region Helpers

    private void RemoveLesserFormActions(EntityUid uid)
    {
        if (!TryComp<ActionsComponent>(uid, out var actionsComponent))
            return;

        foreach (var action in actionsComponent.Actions.ToArray())
        {
            if (!HasComp<LesserFormRestrictedComponent>(action))
                continue;

            _action.RemoveAction(uid, action);
        }
    }

    private void StartUseDelayById(EntityUid performer, string actionProto)
    {
        if (!TryComp<ActionsComponent>(performer, out var actionsComponent))
            return;

        foreach (var action in actionsComponent.Actions.ToArray())
        {
            var id = MetaData(action).EntityPrototype?.ID;

            if (id != actionProto)
                continue;

            _action.StartUseDelay(action);
        }
    }

    private void KillUser(EntityUid target, string damageType)
    {
        if (!_mobThresholdSystem.TryGetThresholdForState(target, MobState.Dead, out var damage))
            return;

        DamageSpecifier dmg = new();
        dmg.DamageDict.Add(damageType, damage.Value);
        _damage.TryChangeDamage(target, dmg, true);
    }

    private void CopyHumanoidData(EntityUid uid, EntityUid target, ChangelingComponent component)
    {
        var data = GetHumanoidData(target, component);

        if (data == null)
            return;

        /*if (component.AbsorbedEntities.Count == 7)
        {
            component.AbsorbedEntities.Remove(component.AbsorbedEntities.ElementAt(2).Key);
        }*/

        component.AbsorbedEntities.Add(data.Value.Dna, data.Value);

        Dirty(uid, component);
    }

    public HumanoidData? GetHumanoidData(EntityUid target, ChangelingComponent? absorberComponent = null)
    {
        if (!TryComp<MetaDataComponent>(target, out var targetMeta))
            return null;

        if (!TryComp<HumanoidAppearanceComponent>(target, out var targetAppearance))
            return null;

        if (!TryComp<DnaComponent>(target, out var targetDna))
            return null;

        if (!TryPrototype(target, out var prototype, targetMeta))
            return null;

        if (absorberComponent != null && absorberComponent.AbsorbedEntities.ContainsKey(targetDna.DNA))
            return null;

        var appearance = _serializationManager.CreateCopy(targetAppearance, notNullableOverride: true);
        var meta = _serializationManager.CreateCopy(targetMeta, notNullableOverride: true);

        var name = string.IsNullOrEmpty(meta.EntityName)
            ? Loc.GetString("changeling-unknown-creature")
            : meta.EntityName;

        return new HumanoidData
        {
            EntityPrototype = prototype,
            MetaDataComponent = meta,
            AppearanceComponent = appearance,
            Name = name,
            Dna = targetDna.DNA
        };
    }

    /// <summary>
    /// Transforms chosen person to another, transferring it's appearance
    /// </summary>
    /// <param name="target">Transform target</param>
    /// <param name="transformData">Transform data</param>
    /// <param name="humanoidOverride">Override first check on HumanoidAppearanceComponent</param>
    /// <returns>Id of the transformed entity</returns>
    public EntityUid? TransformPerson(EntityUid target, HumanoidData transformData, bool humanoidOverride = false)
    {
        if (!HasComp<HumanoidAppearanceComponent>(target) && !humanoidOverride)
            return null;

        BeforeTransform(target);

        var polymorphEntity = _polymorph.PolymorphEntity(target, transformData.EntityPrototype.ID);

        if (polymorphEntity == null)
            return null;

        if (!TryComp<HumanoidAppearanceComponent>(polymorphEntity.Value, out var polyAppearance))
            return null;

        ClonePerson(polymorphEntity.Value, transformData.AppearanceComponent, polyAppearance);
        TransferDna(polymorphEntity.Value, transformData.Dna);

        _humanoidAppearance.SetTTSVoice(polymorphEntity.Value, transformData.AppearanceComponent.Voice, polyAppearance);

        if (!TryComp<MetaDataComponent>(polymorphEntity.Value, out var meta))
            return null;

        _metaData.SetEntityName(polymorphEntity.Value, transformData.MetaDataComponent!.EntityName, meta);
        _metaData.SetEntityDescription(polymorphEntity.Value, transformData.MetaDataComponent!.EntityDescription, meta);

        _identity.QueueIdentityUpdate(polymorphEntity.Value);

        if (TryComp(target, out ChangelingComponent? lingComp))
        {
            var toAdd = new ChangelingComponent
            {
                HiveName = lingComp.HiveName,
                ChemicalCapacity = lingComp.ChemicalCapacity,
                ChemicalsBalance = lingComp.ChemicalsBalance,
                AbsorbedEntities = lingComp.AbsorbedEntities,
                IsInited = lingComp.IsInited,
                AbsorbedCount = lingComp.AbsorbedCount
            };

            EntityManager.AddComponent(polymorphEntity.Value, toAdd);
            _chemicalsSystem.UpdateAlert(polymorphEntity.Value, toAdd);
        }

        TransferComponents(target, polymorphEntity.Value);

        _implantSystem.TransferImplants(target, polymorphEntity.Value);
        _actionContainerSystem.TransferAllActionsFiltered(target, polymorphEntity.Value, polymorphEntity.Value);
        _action.GrantContainedActions(polymorphEntity.Value, polymorphEntity.Value);

        return polymorphEntity;
    }

    private void BeforeTransform(EntityUid target)
    {
        if (TryComp(target, out BorerHostComponent? host) && host.BorerContainer.Count > 0)
            _borer.GetOut(host.BorerContainer.ContainedEntities[0]);

        if (TryComp(target, out BeingCarriedComponent? beingCarried))
            _carrying.DropCarried(beingCarried.Carrier, target);
    }

    private void TransferComponents(EntityUid from, EntityUid to)
    {
        if (HasComp<FastAndFuriousComponent>(from))
            EnsureComp<FastAndFuriousComponent>(to);

        if (HasComp<AbsorbedComponent>(from))
            EnsureComp<AbsorbedComponent>(to);

        if (HasComp<UncloneableComponent>(from))
            EnsureComp<UncloneableComponent>(to);

        if (HasComp<BibleUserComponent>(from))
            EnsureComp<BibleUserComponent>(to);

        if (HasComp<FlashImmunityComponent>(from))
            EnsureComp<FlashImmunityComponent>(to);

        if (HasComp<EyeProtectionComponent>(from))
            EnsureComp<EyeProtectionComponent>(to);

        if (HasComp<VoidAdaptationComponent>(from))
            EnsureComp<VoidAdaptationComponent>(to);

        if (TryComp(from, out TemporaryNightVisionComponent? nvComp))
        {
            var toAdd = new TemporaryNightVisionComponent
            {
                Color = nvComp.Color,
                Tint = nvComp.Tint,
                Strength = nvComp.Strength,
                Noise = nvComp.Noise
            };

            EntityManager.AddComponent(to, toAdd);
        }

        if (TryComp(from, out NpcFactionMemberComponent? factionMember))
        {
            _faction.ClearFactions(to);
            foreach (var faction in factionMember.Factions)
            {
                _faction.AddFaction(to, faction);
            }
        }

        if (TryComp(from, out MoodComponent? mood))
        {
            var newMood = EnsureComp<MoodComponent>(to);
            foreach (var effect in mood.CategorisedEffects)
            {
                _mood.ApplyEffect(to, newMood, effect.Value);
            }

            foreach (var effect in mood.UncategorisedEffects)
            {
                _mood.ApplyEffect(to, newMood, effect.Key);
            }
        }

        _rev.TransferRole(from, to);

        _nukeOps.TransferRole(from, to);

        _cult.TransferRole(from, to);
    }

    private void TransferDna(EntityUid target, string dna)
    {
        if (!TryComp<DnaComponent>(target, out var dnaComponent))
            return;

        dnaComponent.DNA = dna;
    }

    private void TryTransformChangeling(EntityUid uid, string dna, ChangelingComponent component)
    {
        if (!component.AbsorbedEntities.TryGetValue(dna, out var person))
            return;

        EntityUid? reverted = uid;

        reverted = component.IsLesserForm
            ? TransformPerson(reverted.Value, person, humanoidOverride: true)
            : TransformPerson(reverted.Value, person);

        if (reverted == null)
            return;

        if (component.IsLesserForm)
        {
            //Don't copy IsLesserForm bool, because transferred component, in fact, new. Bool default value if false.
            StartUseDelayById(reverted.Value, ChangelingLesserForm);
        }

        StartUseDelayById(reverted.Value, ChangelingTransform);
    }

    /// <summary>
    /// Used for cloning appearance
    /// </summary>
    /// <param name="target">Acceptor</param>
    /// <param name="sourceHumanoid">Source appearance</param>
    /// <param name="targetHumanoid">Acceptor appearance component</param>
    private void ClonePerson(
        EntityUid target,
        HumanoidAppearanceComponent sourceHumanoid,
        HumanoidAppearanceComponent targetHumanoid)
    {
        targetHumanoid.Species = sourceHumanoid.Species;
        targetHumanoid.SkinColor = sourceHumanoid.SkinColor;
        targetHumanoid.EyeColor = sourceHumanoid.EyeColor;
        targetHumanoid.Age = sourceHumanoid.Age;
        _humanoidAppearance.SetSex(target, sourceHumanoid.Sex, false, targetHumanoid);
        _humanoidAppearance.SetBodyType(target, sourceHumanoid.BodyType, false, targetHumanoid);
        _humanoidAppearance.SetSpecies(target, sourceHumanoid.Species, true, targetHumanoid);
        targetHumanoid.CustomBaseLayers = new Dictionary<HumanoidVisualLayers,
            CustomBaseLayerInfo>(sourceHumanoid.CustomBaseLayers);

        targetHumanoid.MarkingSet = new MarkingSet(sourceHumanoid.MarkingSet);

        targetHumanoid.Gender = sourceHumanoid.Gender;
        if (TryComp<GrammarComponent>(target, out var grammar))
        {
            grammar.Gender = sourceHumanoid.Gender;
        }

        Dirty(target, targetHumanoid);
    }

    private void SpawnOrDeleteItem(EntityUid target, ChangelingComponent component, string prototypeName, int chemicals)
    {
        foreach (var eHand in _handsSystem.EnumerateHands(target))
        {
            if (eHand.HeldEntity == null || !TryComp<MetaDataComponent>(eHand.HeldEntity.Value, out var meta))
                continue;

            if (meta.EntityPrototype != null && meta.EntityPrototype.ID != prototypeName)
                continue;

            Del(eHand.HeldEntity);
            return;
        }

        if (!_handsSystem.TryGetEmptyHand(target, out var hand))
        {
            _popup.PopupEntity(Loc.GetString("changeling-popup-need-hand"), target, target);
            return;
        }

        if (!TakeChemicals(target, component, chemicals))
            return;

        var item = Spawn(prototypeName, Transform(target).Coordinates);

        if (!_handsSystem.TryPickup(target, item, hand, animate: false))
        {
            Del(item);
        }
    }

    private bool TakeChemicals(EntityUid uid, ChangelingComponent component, int quantity)
    {
        if (!_chemicalsSystem.RemoveChemicals(uid, component, quantity))
        {
            _popup.PopupEntity(Loc.GetString("changeling-popup-lack-chemicals"), uid, uid);
            return false;
        }

        _popup.PopupEntity(Loc.GetString("changeling-popup-used-chemicals", ("quantity", quantity)), uid, uid);

        return true;
    }

    private void AbsorbLing(EntityUid uid, ChangelingComponent changelingComponent)
    {
        changelingComponent.ChemicalCapacity += 40;

        if (!TryComp<ImplantedComponent>(uid, out var implant))
            return;

        foreach (var entity in implant.ImplantContainer.ContainedEntities)
        {
            if (!TryComp<StoreComponent>(entity, out var store))
                continue;

            var toAdd = new Dictionary<string, FixedPoint2> { { "ChangelingPoint", 5 } };
            _storeSystem.TryAddCurrency(toAdd, entity, store);

            return;
        }
    }

#endregion
}
