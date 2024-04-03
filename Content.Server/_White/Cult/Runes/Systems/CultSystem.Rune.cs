using System.Linq;
using System.Numerics;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Components;
using Content.Server.Chat.Systems;
using Content.Server.Body.Systems;
using Content.Server.DoAfter;
using Content.Server.Hands.Systems;
using Content.Server.Weapons.Ranged.Systems;
using Content.Server._White.Cult.GameRule;
using Content.Server._White.Cult.Runes.Comps;
using Content.Server._White.Cult.UI;
using Content.Server.Bible.Components;
using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.Fluids.Components;
using Content.Server.Ghost;
using Content.Server.Revenant.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Cuffs.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Humanoid;
using Content.Shared.Interaction.Events;
using Content.Shared.Maps;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Pulling.Components;
using Content.Shared.Rejuvenate;
using Content.Shared._White.Cult;
using Content.Shared._White.Cult.Components;
using Content.Shared._White.Cult.Runes;
using Content.Shared._White.Cult.UI;
using Content.Shared.Cuffs;
using Content.Shared.GameTicking;
using Content.Shared.Mindshield.Components;
using Content.Shared.Pulling;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Random;
using CultistComponent = Content.Shared._White.Cult.Components.CultistComponent;

namespace Content.Server._White.Cult.Runes.Systems;

public sealed partial class CultSystem : EntitySystem
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly BodySystem _bodySystem = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly CultRuleSystem _ruleSystem = default!;
    [Dependency] private readonly HandsSystem _handsSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly GunSystem _gunSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly FlammableSystem _flammableSystem = default!;
    [Dependency] private readonly SharedPullingSystem _pulling = default!;
    [Dependency] private readonly SharedCuffableSystem _cuffable = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;


    public override void Initialize()
    {
        base.Initialize();

        // Runes
        SubscribeLocalEvent<CultRuneBaseComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<CultRuneOfferingComponent, CultRuneInvokeEvent>(OnInvokeOffering);
        SubscribeLocalEvent<CultRuneBuffComponent, CultRuneInvokeEvent>(OnInvokeBuff);
        SubscribeLocalEvent<CultRuneTeleportComponent, CultRuneInvokeEvent>(OnInvokeTeleport);
        SubscribeLocalEvent<CultRuneApocalypseComponent, CultRuneInvokeEvent>(OnInvokeApocalypse);
        SubscribeLocalEvent<CultRuneReviveComponent, CultRuneInvokeEvent>(OnInvokeRevive);
        SubscribeLocalEvent<CultRuneBarrierComponent, CultRuneInvokeEvent>(OnInvokeBarrier);
        SubscribeLocalEvent<CultRuneSummoningComponent, CultRuneInvokeEvent>(OnInvokeSummoning);
        SubscribeLocalEvent<CultRuneBloodBoilComponent, CultRuneInvokeEvent>(OnInvokeBloodBoil);
        SubscribeLocalEvent<CultistComponent, SummonNarsieDoAfterEvent>(NarsieSpawn);

        SubscribeLocalEvent<CultEmpowerComponent, CultEmpowerSelectedBuiMessage>(OnEmpowerSelected);
        SubscribeLocalEvent<CultEmpowerComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<CultEmpowerComponent, ActivateInWorldEvent>(OnActiveInWorld);

        // UI
        SubscribeLocalEvent<RuneDrawerProviderComponent, ActivatableUIOpenAttemptEvent>(OnRuneDrawAttempt);
        SubscribeLocalEvent<RuneDrawerProviderComponent, BeforeActivatableUIOpenEvent>(BeforeRuneDraw);
        SubscribeLocalEvent<RuneDrawerProviderComponent, ListViewItemSelectedMessage>(OnRuneSelected);
        SubscribeLocalEvent<CultTeleportRuneProviderComponent, TeleportRunesListWindowItemSelectedMessage>(
            OnTeleportRuneSelected);

        SubscribeLocalEvent<CultRuneSummoningProviderComponent, SummonCultistListWindowItemSelectedMessage>(
            OnCultistSelected);

        // Rune drawing/erasing
        SubscribeLocalEvent<CultistComponent, CultDrawEvent>(OnDraw);
        SubscribeLocalEvent<CultistComponent, NameSelectorMessage>(OnChoose);
        SubscribeLocalEvent<CultRuneBaseComponent, InteractUsingEvent>(TryErase);
        SubscribeLocalEvent<CultRuneBaseComponent, CultEraseEvent>(OnErase);
        SubscribeLocalEvent<CultRuneBaseComponent, StartCollideEvent>(HandleCollision);

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);

        InitializeBuffSystem();
        InitializeNarsie();
        InitializeSoulShard();
        InitializeConstructs();
        InitializeBarrierSystem();
        InitializeConstructsAbilities();
        InitializeActions();
        InitializeVerb();
    }

    private float _timeToDraw;

    private const string TeleportRunePrototypeId = "TeleportRune";
    private const string ApocalypseRunePrototypeId = "ApocalypseRune";
    private const string RitualDaggerPrototypeId = "RitualDagger";
    private const string RunicMetalPrototypeId = "CultRunicMetal";
    private const string SteelPrototypeId = "Steel";
    private const string NarsiePrototypeId = "Narsie";
    private const string CultBarrierPrototypeId = "CultBarrier";

    private bool _doAfterAlreadyStarted;

    private readonly SoundPathSpecifier _teleportInSound = new("/Audio/White/Cult/veilin.ogg");
    private readonly SoundPathSpecifier _teleportOutSound = new("/Audio/White/Cult/veilout.ogg");

    private readonly SoundPathSpecifier _magic = new("/Audio/White/Cult/magic.ogg");

    private readonly SoundPathSpecifier _apocRuneEndDrawing = new("/Audio/White/Cult/finisheddraw.ogg");
    private readonly SoundPathSpecifier _narsie40Sec = new("/Audio/White/Cult/40sec.ogg");

    private Entity<AudioComponent>? _narsieSummonningAudio = null;

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        CultRuneReviveComponent.ChargesLeft = 3;
    }

    /*
    * Rune draw start ----
     */

    private void OnRuneDrawAttempt(Entity<RuneDrawerProviderComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (!HasComp<CultistComponent>(args.User))
            args.Cancel();
    }

    private void BeforeRuneDraw(Entity<RuneDrawerProviderComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        if (_ui.TryGetUi(ent, ListViewSelectorUiKey.Key, out var bui))
            _ui.SetUiState(bui, new ListViewBUIState(ent.Comp.RunePrototypes, true));
    }

    private void OnRuneSelected(EntityUid uid, RuneDrawerProviderComponent component, ListViewItemSelectedMessage args)
    {
        if (args.Session.AttachedEntity == null)
            return;

        var runePrototype = args.SelectedItem;
        var whoCalled = args.Session.AttachedEntity.Value;

        if (!TryComp<ActorComponent>(whoCalled, out var actorComponent))
            return;

        if (!TryDraw(whoCalled, runePrototype))
            return;

        /*if (component.UserInterface != null)
            _ui.CloseUi(component.UserInterface, actorComponent.PlayerSession);*/
    }

    private bool TryDraw(EntityUid whoCalled, string runePrototype)
    {
        _timeToDraw = 4f;

        if (HasComp<CultBuffComponent>(whoCalled))
            _timeToDraw /= 2;

        if (!IsAllowedToDraw(whoCalled))
            return false;

        if (runePrototype == ApocalypseRunePrototypeId)
        {
            if (!_mindSystem.TryGetMind(whoCalled, out _, out var mind) ||
                mind.Session is not { } playerSession)
                return false;

            _euiManager.OpenEui(new ApocalypseRuneEui(whoCalled, _entityManager), playerSession);

            return true;
        }

        var ev = new CultDrawEvent
        {
            Rune = runePrototype
        };

        var argsDoAfterEvent = new DoAfterArgs(_entityManager, whoCalled, _timeToDraw, ev, whoCalled)
        {
            BreakOnUserMove = true,
            NeedHand = true
        };

        if (!_doAfterSystem.TryStartDoAfter(argsDoAfterEvent))
            return false;

        _audio.PlayPvs("/Audio/White/Cult/butcher.ogg", whoCalled, AudioParams.Default.WithMaxDistance(2f));
        return true;
    }

    private void OnDraw(EntityUid uid, CultistComponent comp, CultDrawEvent args)
    {
        if (args.Cancelled)
            return;

        var howMuchBloodTake = -10;
        var rune = args.Rune;
        var user = args.User;

        if (HasComp<CultBuffComponent>(user))
            howMuchBloodTake /= 2;

        if (!TryComp<BloodstreamComponent>(user, out var bloodstreamComponent))
            return;

        _bloodstreamSystem.TryModifyBloodLevel(user, howMuchBloodTake, bloodstreamComponent, createPuddle: false);
        _audio.PlayPvs("/Audio/White/Cult/blood.ogg", user, AudioParams.Default.WithMaxDistance(2f));

        if (rune == TeleportRunePrototypeId)
        {
            if (!TryComp<ActorComponent>(user, out var actorComponent))
                return;

            if (_ui.TryGetUi(user, NameSelectorUIKey.Key, out var bui))
            {
                _ui.OpenUi(bui, actorComponent.PlayerSession);
            }

            return;
        }

        SpawnRune(user, rune);
    }

    private void OnChoose(EntityUid uid, CultistComponent component, NameSelectorMessage args)
    {
        if (!TryComp<ActorComponent>(uid, out var actorComponent))
            return;

        if (_ui.TryGetUi(uid, NameSelectorUIKey.Key, out var bui))
        {
            _ui.CloseUi(bui, actorComponent.PlayerSession);
        }

        SpawnRune(uid, TeleportRunePrototypeId, true, args.Name);
    }

    //Erasing start

    private void TryErase(EntityUid uid, CultRuneBaseComponent component, InteractUsingEvent args)
    {
        if (TryComp<BibleComponent>(args.Used, out var bible) && HasComp<BibleUserComponent>(args.User))
        {
            _popupSystem.PopupEntity(Loc.GetString("cult-erased-rune"), args.User, args.User);
            _audio.PlayPvs(bible.HealSoundPath, args.User);
            EntityManager.DeleteEntity(args.Target);
            return;
        }

        var entityPrototype = _entityManager.GetComponent<MetaDataComponent>(args.Used).EntityPrototype;

        if (entityPrototype == null)
            return;

        var used = entityPrototype.ID;
        var user = args.User;
        var target = args.Target;
        var time = 3;

        if (used != RitualDaggerPrototypeId)
            return;

        if (!HasComp<CultistComponent>(user))
            return;

        if (HasComp<CultBuffComponent>(user))
            time /= 2;

        var netEntity = GetNetEntity(target);

        var ev = new CultEraseEvent
        {
            TargetEntityId = netEntity
        };

        var argsDoAfterEvent = new DoAfterArgs(_entityManager, user, time, ev, target)
        {
            BreakOnUserMove = true,
            NeedHand = true
        };

        if (_doAfterSystem.TryStartDoAfter(argsDoAfterEvent))
        {
            _popupSystem.PopupEntity(Loc.GetString("cult-started-erasing-rune"), args.User, args.User);
        }
    }

    private void OnErase(EntityUid uid, CultRuneBaseComponent component, CultEraseEvent args)
    {
        if (args.Cancelled)
            return;

        var target = GetEntity(args.TargetEntityId);

        _entityManager.DeleteEntity(target);
        _popupSystem.PopupEntity(Loc.GetString("cult-erased-rune"), args.User, args.User);
    }

    private void HandleCollision(EntityUid uid, CultRuneBaseComponent component, ref StartCollideEvent args)
    {
        if (!TryComp<SolutionContainerManagerComponent>(args.OtherEntity, out var solution) ||
            !HasComp<VaporComponent>(args.OtherEntity) && !HasComp<SprayComponent>(args.OtherEntity))
        {
            return;
        }

        var solutions = _solutionContainerSystem.EnumerateSolutions((args.OtherEntity, solution));

        if (solutions.Any(x => x.Solution.Comp.Solution.ContainsPrototype(CultRuleComponent.HolyWaterReagent)))
        {
            Del(uid);
        }
    }

    //Erasing end

    /*
    * Rune draw end ----
     */

    //------------------------------------------//

    /*
     * Base Start ----
     */

    private void OnActivate(EntityUid uid, CultRuneBaseComponent component, ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (!HasComp<CultistComponent>(args.User))
            return;

        var cultists = new HashSet<EntityUid>
        {
            args.User
        };

        if (component.InvokersMinCount > 1 || component.GatherInvokers)
            cultists = GatherCultists(uid, component.CultistGatheringRange);

        if (cultists.Count < component.InvokersMinCount)
        {
            _popupSystem.PopupEntity(Loc.GetString("not-enough-cultists"), args.User, args.User);
            return;
        }

        var ev = new CultRuneInvokeEvent(uid, args.User, cultists);
        RaiseLocalEvent(uid, ev);

        if (ev.Result)
        {
            OnAfterInvoke(uid, cultists);
        }
    }

    private void OnAfterInvoke(EntityUid rune, HashSet<EntityUid> cultists)
    {
        if (!_entityManager.TryGetComponent<CultRuneBaseComponent>(rune, out var component))
            return;

        foreach (var cultist in cultists)
        {
            _chat.TrySendInGameICMessage(cultist, component.InvokePhrase, InGameICChatType.Speak, false, false, null,
                null, null, false);
        }
    }

    /*
    * Base End ----
    */

    //------------------------------------------//

    /*
     * Offering Rune START ----
     */

    private void OnInvokeOffering(EntityUid uid, CultRuneOfferingComponent component, CultRuneInvokeEvent args)
    {
        var targets =
            _lookup.GetEntitiesInRange(uid, component.RangeTarget, LookupFlags.Dynamic | LookupFlags.Sundries);

        targets.RemoveWhere(x =>
            !_entityManager.HasComponent<HumanoidAppearanceComponent>(x) || HasComp<CultistComponent>(x));

        if (targets.Count == 0)
            return;

        var victim = FindNearestTarget(uid, targets.ToList());

        if (victim == null)
            return;

        _entityManager.TryGetComponent<MobStateComponent>(victim.Value, out var state);

        if (state == null)
            return;

        bool result;

        if (state.CurrentState != MobState.Dead)
        {
            var canBeConverted = _entityManager.TryGetComponent<MindContainerComponent>(victim.Value, out var mind) &&
                                 mind.Mind != null && !IsTarget(mind.Mind.Value);

            // Выполнение действия в зависимости от условий
            if (canBeConverted && !HasComp<BibleUserComponent>(victim.Value) &&
                !HasComp<MindShieldComponent>(victim.Value))
            {
                result = Convert(uid, victim.Value, args.User, args.Cultists);
            }
            else
            {
                result = Sacrifice(uid, victim.Value, args.User, args.Cultists);
            }
        }
        else
        {
            // Жертва мертва, выполняется альтернативное действие
            result = SacrificeNonObjectiveDead(uid, victim.Value, args.User, args.Cultists);
        }

        args.Result = result;
    }

    private bool IsTarget(EntityUid mindId)
    {
        var target = _ruleSystem.GetTarget();
        if (target == null)
            return false;

        return mindId == target.Value.Owner;
    }

    private bool Sacrifice(
        EntityUid rune,
        EntityUid target,
        EntityUid user,
        HashSet<EntityUid> cultists)
    {
        if (!_entityManager.TryGetComponent<CultRuneOfferingComponent>(rune, out var offering))
            return false;

        if (cultists.Count < offering.SacrificeMinCount)
        {
            _popupSystem.PopupEntity(Loc.GetString("cult-sacrifice-not-enough-cultists"), user, user);
            return false;
        }

        if (!SpawnShard(target))
        {
            _bodySystem.GibBody(target);
        }

        AddChargesToReviveRune();
        return true;
    }

    private bool SacrificeNonObjectiveDead(
        EntityUid rune,
        EntityUid target,
        EntityUid user,
        HashSet<EntityUid> cultists)
    {
        if (!_entityManager.TryGetComponent<CultRuneOfferingComponent>(rune, out var offering))
            return false;

        if (cultists.Count < offering.SacrificeDeadMinCount)
        {
            _popupSystem.PopupEntity(Loc.GetString("cult-sacrifice-not-enough-cultists"), user, user);
            return false;
        }

        if (!SpawnShard(target))
        {
            _bodySystem.GibBody(target);
        }

        AddChargesToReviveRune();
        return true;
    }

    private bool Convert(EntityUid rune, EntityUid target, EntityUid user, HashSet<EntityUid> cultists)
    {
        if (!_entityManager.TryGetComponent<CultRuneOfferingComponent>(rune, out var offering))
            return false;

        if (cultists.Count < offering.ConvertMinCount)
        {
            _popupSystem.PopupEntity(Loc.GetString("cult-offering-rune-not-enough"), user, user);
            return false;
        }

        if (!_entityManager.TryGetComponent<ActorComponent>(target, out var actorComponent))
            return false;

        _stunSystem.TryStun(target, TimeSpan.FromSeconds(2f), false);
        _ruleSystem.MakeCultist(actorComponent.PlayerSession);
        HealCultist(target);

        if (TryComp(target, out CuffableComponent? cuffs) && cuffs.Container.ContainedEntities.Count >= 1)
        {
            var lastAddedCuffs = cuffs.LastAddedCuffs;
            _cuffable.Uncuff(target, user, lastAddedCuffs);
        }

        _statusEffectsSystem.TryRemoveStatusEffect(target, "Muted");

        RemCompDeferred<BlightComponent>(target);

        return true;
    }

    /*
     * Offering Rune END ----
     */

    //------------------------------------------//

    /*
    * Buff Rune Start ----
     */

    private void OnInvokeBuff(EntityUid uid, CultRuneBuffComponent component, CultRuneInvokeEvent args)
    {
        var targets =
            _lookup.GetEntitiesInRange(uid, component.RangeTarget, LookupFlags.Dynamic | LookupFlags.Sundries);

        targets.RemoveWhere(x =>
            !_entityManager.HasComponent<HumanoidAppearanceComponent>(x) ||
            !_entityManager.HasComponent<CultistComponent>(x));

        if (targets.Count == 0)
            return;

        var victim = FindNearestTarget(uid, targets.ToList());

        if (victim == null)
            return;

        _entityManager.TryGetComponent<MobStateComponent>(victim.Value, out var state);

        var result = false;

        if (state != null && state.CurrentState != MobState.Dead)
        {
            result = AddCultistBuff(victim.Value, args.User);
        }

        args.Result = result;
    }

    private bool AddCultistBuff(EntityUid target, EntityUid user)
    {
        if (HasComp<CultBuffComponent>(target))
        {
            _popupSystem.PopupEntity(Loc.GetString("cult-buff-already-buffed"), user, user);
            return false;
        }

        EnsureComp<CultBuffComponent>(target);
        return true;
    }

    /*
    * Empower Rune End ----
     */

    //------------------------------------------//

    /*
    * Teleport rune start ----
     */

    private void OnInvokeTeleport(EntityUid uid, CultRuneTeleportComponent component, CultRuneInvokeEvent args)
    {
        var targets =
            _lookup.GetEntitiesInRange(uid, component.RangeTarget, LookupFlags.Dynamic | LookupFlags.Sundries);

        if (targets.Count == 0)
        {
            return;
        }

        args.Result = Teleport(uid, args.User, targets.ToList());
    }

    private bool Teleport(EntityUid rune, EntityUid user, List<EntityUid>? victims = null)
    {
        var runes = EntityQuery<CultRuneTeleportComponent>();
        var list = new List<int>();
        var labels = new List<string>();

        foreach (var teleportRune in runes)
        {
            if (!TryComp<CultRuneTeleportComponent>(teleportRune.Owner, out var teleportComponent))
                continue;

            if (teleportComponent.Label == null)
                continue;

            if (teleportRune.Owner == rune)
                continue;

            if (!int.TryParse(teleportRune.Owner.ToString(), out var intValue))
                continue;

            list.Add(intValue);
            labels.Add(teleportComponent.Label);
        }

        if (!TryComp<ActorComponent>(user, out var actorComponent))
            return false;

        var ui = _ui.GetUiOrNull(user, RuneTeleporterUiKey.Key);

        if (ui == null)
            return false;

        if (list.Count == 0)
        {
            _popupSystem.PopupEntity(Loc.GetString("cult-teleport-rune-not-found"), user, user);
            return false;
        }

        _entityManager.EnsureComponent<CultTeleportRuneProviderComponent>(user, out var providerComponent);
        providerComponent.Targets = victims;
        providerComponent.BaseRune = rune;

        _ui.SetUiState(ui, new TeleportRunesListWindowBUIState(list, labels));

        if (_ui.IsUiOpen(user, ui.UiKey))
            return false;

        _ui.ToggleUi(ui, actorComponent.PlayerSession);
        return true;
    }

    private void OnTeleportRuneSelected(
        EntityUid uid,
        CultTeleportRuneProviderComponent component,
        TeleportRunesListWindowItemSelectedMessage args)
    {
        var targets = component.Targets;
        var user = args.Session.AttachedEntity;
        var selectedRune = new EntityUid(args.SelectedItem);
        var baseRune = component.BaseRune;

        if (targets is null || targets.Count == 0)
            return;

        if (user == null || baseRune == null)
            return;

        if (!TryComp<TransformComponent>(selectedRune, out var xFormSelected) ||
            !TryComp<TransformComponent>(baseRune, out var xFormBase))
            return;

        foreach (var target in targets)
        {
            StopPulling(target);

            _xform.SetCoordinates(target, xFormSelected.Coordinates);
        }

        //Play tp sound
        _audio.PlayPvs(_teleportInSound, xFormSelected.Coordinates);
        _audio.PlayPvs(_teleportOutSound, xFormBase.Coordinates);

        if (HasComp<CultTeleportRuneProviderComponent>(user.Value))
        {
            RemComp<CultTeleportRuneProviderComponent>(user.Value);
        }
    }

    /*
    * Teleport rune end ----
     */

    //------------------------------------------//

    /*
    * Apocalypse rune start ----
     */

    private void OnInvokeApocalypse(EntityUid uid, CultRuneApocalypseComponent component, CultRuneInvokeEvent args)
    {
        args.Result = TrySummonNarsie(args.User, args.Cultists, component);
    }

    private bool TrySummonNarsie(EntityUid user, HashSet<EntityUid> cultists, CultRuneApocalypseComponent component)
    {
        var canSummon = _ruleSystem.CanSummonNarsie();

        if (!canSummon)
        {
            _popupSystem.PopupEntity(Loc.GetString("cult-narsie-not-completed-tasks"), user, user);
            return false;
        }

        if (cultists.Count < component.SummonMinCount)
        {
            _popupSystem.PopupEntity(Loc.GetString("cult-narsie-summon-not-enough"), user, user);
            return false;
        }

        if (_doAfterAlreadyStarted)
        {
            _popupSystem.PopupEntity(Loc.GetString("cult-narsie-already-summoning"), user, user);
            return false;
        }

        if (!TryComp<DoAfterComponent>(user, out var doAfterComponent))
        {
            if (doAfterComponent is { AwaitedDoAfters.Count: >= 1 })
            {
                _popupSystem.PopupEntity(Loc.GetString("cult-narsie-summon-do-after"), user, user);
                return false;
            }
        }

        var ev = new SummonNarsieDoAfterEvent();

        var argsDoAfterEvent = new DoAfterArgs(_entityManager, user, TimeSpan.FromSeconds(40), ev, user)
        {
            BreakOnUserMove = true
        };

        if (!_doAfterSystem.TryStartDoAfter(argsDoAfterEvent))
            return false;

        _popupSystem.PopupEntity(Loc.GetString("cult-stay-still"), user, user, PopupType.LargeCaution);

        _doAfterAlreadyStarted = true;

        _chat.DispatchGlobalAnnouncement(Loc.GetString("cult-ritual-started"), "CULT", false,
            colorOverride: Color.DarkRed);

        _narsieSummonningAudio = _audio.PlayGlobal(_narsie40Sec, Filter.Broadcast(), false, AudioParams.Default.WithLoop(true).WithVolume(0.15f));

        return true;
    }

    private void NarsieSpawn(EntityUid uid, CultistComponent component, SummonNarsieDoAfterEvent args)
    {
        _doAfterAlreadyStarted = false;

        _audio.Stop(_narsieSummonningAudio?.Owner, _narsieSummonningAudio?.Comp);

        if (args.Cancelled)
        {
            _chat.DispatchGlobalAnnouncement(Loc.GetString("cult-ritual-prevented"), "CULT", false,
                colorOverride: Color.DarkRed);

            return;
        }

        var transform = CompOrNull<TransformComponent>(args.User)?.Coordinates;
        if (transform == null)
            return;

        var ev = new CultNarsieSummoned();
        RaiseLocalEvent(ev);

        _entityManager.SpawnEntity(NarsiePrototypeId, transform.Value);

        //_chat.DispatchGlobalAnnouncement(Loc.GetString("cult-narsie-summoned"), "CULT", true, _apocRuneEndDrawing,
        //    colorOverride: Color.DarkRed);
    }

    /*
    * Apocalypse rune end ----
     */

    //------------------------------------------//

    /*
   * Revive rune start ----
       */

    private void OnInvokeRevive(EntityUid uid, CultRuneReviveComponent component, CultRuneInvokeEvent args)
    {
        var targets =
            _lookup.GetEntitiesInRange(uid, component.RangeTarget, LookupFlags.Dynamic | LookupFlags.Sundries);

        targets.RemoveWhere(x =>
            !_entityManager.HasComponent<HumanoidAppearanceComponent>(x) || !HasComp<CultistComponent>(x));

        if (targets.Count == 0)
            return;

        var victim = FindNearestTarget(uid, targets.ToList());

        if (victim == null)
            return;

        _entityManager.TryGetComponent<MobStateComponent>(victim.Value, out var state);

        if (state == null)
            return;

        if (state.CurrentState != MobState.Dead && state.CurrentState != MobState.Critical)
        {
            _popupSystem.PopupEntity(Loc.GetString("cult-revive-rune-already-alive"), args.User, args.User);
            return;
        }

        var result = Revive(victim.Value, args.User);

        args.Result = result;
    }

    private bool Revive(EntityUid target, EntityUid user)
    {
        if (CultRuneReviveComponent.ChargesLeft == 0)
        {
            _popupSystem.PopupEntity(Loc.GetString("cult-revive-rune-no-charges"), user, user);
            return false;
        }

        CultRuneReviveComponent.ChargesLeft--;

        _entityManager.EventBus.RaiseLocalEvent(target, new RejuvenateEvent());

        EntityUid? transferTo = null;

        if (!_mindSystem.TryGetMind(target, out var mindId, out var mind))
        {
            if (!TryComp<CultistComponent>(target, out var cultist) || cultist.OriginalMind == null)
                return true;

            (mindId, mind) = cultist.OriginalMind.Value;

            transferTo = target;
        }

        if (mind.Session is not { } playerSession)
            return true;

        // notify them they're being revived.
        if (mind.CurrentEntity != target)
        {
            _euiManager.OpenEui(new ReturnToBodyEui(mind, _mindSystem, mindId, transferTo), playerSession);
        }
        return true;
    }

    /*
* Revive rune end ----
    */

    //------------------------------------------//

    /*
    * Barrier rune start ----
     */

    private void OnInvokeBarrier(EntityUid uid, CultRuneBarrierComponent component, CultRuneInvokeEvent args)
    {
        args.Result = SpawnBarrier(args.Rune);
    }

    private bool SpawnBarrier(EntityUid rune)
    {
        var transform = CompOrNull<TransformComponent>(rune)?.Coordinates;

        if (transform == null)
            return false;

        _entityManager.SpawnEntity(CultBarrierPrototypeId, transform.Value);
        _entityManager.DeleteEntity(rune);

        return true;
    }

    /*
    * Barrier rune end ----
    */

    //------------------------------------------//

    /*
   * Summoning rune start ----
    */

    private void OnInvokeSummoning(EntityUid uid, CultRuneSummoningComponent component, CultRuneInvokeEvent args)
    {
        args.Result = Summon(uid, args.User, args.Cultists, component);
    }

    private bool Summon(
        EntityUid rune,
        EntityUid user,
        HashSet<EntityUid> cultistHashSet,
        CultRuneSummoningComponent component)
    {
        var cultists = EntityQuery<CultistComponent>();
        var list = new List<int>();
        var labels = new List<string>();

        if (cultistHashSet.Count < component.SummonMinCount)
        {
            _popupSystem.PopupEntity(Loc.GetString("cult-summon-rune-need-minimum-cultists"), user, user);
            return false;
        }

        foreach (var cultist in cultists)
        {
            if (!TryComp<MetaDataComponent>(cultist.Owner, out var meta))
                continue;

            if (cultistHashSet.Contains(cultist.Owner))
                continue;

            if (!int.TryParse(cultist.Owner.ToString(), out var intValue))
                continue;

            list.Add(intValue);
            labels.Add(meta.EntityName);
        }

        if (!TryComp<ActorComponent>(user, out var actorComponent))
            return false;

        var ui = _ui.GetUiOrNull(user, SummonCultistUiKey.Key);

        if (ui == null)
            return false;

        if (list.Count == 0)
        {
            _popupSystem.PopupEntity(Loc.GetString("cult-cultists-not-found"), user, user);
            return false;
        }

        _entityManager.EnsureComponent<CultRuneSummoningProviderComponent>(user, out var providerComponent);
        providerComponent.BaseRune = rune;

        _ui.SetUiState(ui, new SummonCultistListWindowBUIState(list, labels));

        if (_ui.IsUiOpen(user, ui.UiKey))
            return false;

        _ui.ToggleUi(ui, actorComponent.PlayerSession);
        return true;
    }

    private void OnCultistSelected(
        EntityUid uid,
        CultRuneSummoningProviderComponent component,
        SummonCultistListWindowItemSelectedMessage args)
    {
        var user = args.Session.AttachedEntity;
        var target = new EntityUid(args.SelectedItem);
        var baseRune = component.BaseRune;

        if (!TryComp<SharedPullableComponent>(target, out var pullableComponent))
            return;

        if (!TryComp<CuffableComponent>(target, out var cuffableComponent))
            return;

        if (user == null || baseRune == null)
            return;

        if (!TryComp<TransformComponent>(baseRune, out var xFormBase))
            return;

        var isCuffed = cuffableComponent.CuffedHandCount > 0;
        var isPulled = pullableComponent.BeingPulled;

        if (isPulled)
        {
            _popupSystem.PopupEntity("Его кто-то держит!", user.Value);
            return;
        }

        if (isCuffed)
        {
            _popupSystem.PopupEntity("Он в наручниках!", user.Value);
            return;
        }

        StopPulling(target, false);

        _xform.SetCoordinates(target, xFormBase.Coordinates);

        _audio.PlayPvs(_teleportInSound, xFormBase.Coordinates);

        if (HasComp<CultRuneSummoningProviderComponent>(user.Value))
        {
            RemComp<CultRuneSummoningProviderComponent>(user.Value);
        }
    }

    /*
   * Summoning rune end ----
    */

    //------------------------------------------//

    /*
   * BloodBoil rune start ----
    */

    private void OnInvokeBloodBoil(EntityUid uid, CultRuneBloodBoilComponent component, CultRuneInvokeEvent args)
    {
        args.Result = PrepareShoot(uid, args.User, args.Cultists, 1.0f, component);
    }

    private bool PrepareShoot(
        EntityUid rune,
        EntityUid user,
        HashSet<EntityUid> cultists,
        float severity,
        CultRuneBloodBoilComponent component)
    {
        cultists = cultists.Where(HasComp<CultistComponent>).ToHashSet(); // Prevent constructs from using the rune

        if (cultists.Count < component.SummonMinCount)
        {
            _popupSystem.PopupEntity(Loc.GetString("cult-blood-boil-rune-need-minimum"), user, user);
            return false;
        }

        var xformQuery = GetEntityQuery<TransformComponent>();
        var xform = xformQuery.GetComponent(rune);

        var inRange = _lookup.GetEntitiesInRange(rune, component.ProjectileRange * severity, LookupFlags.Dynamic);
        inRange.RemoveWhere(x =>
            !_entityManager.HasComponent<HumanoidAppearanceComponent>(x) ||
            _entityManager.HasComponent<CultistComponent>(x));

        var list = inRange.ToList();

        if (list.Count == 0)
        {
            _popupSystem.PopupEntity(Loc.GetString("cult-blood-boil-rune-no-targets"), user, user);
            return false;
        }

        _random.Shuffle(list);

        var bloodCost = 120 / cultists.Count;

        foreach (var cultist in cultists)
        {
            if (!TryComp<BloodstreamComponent>(cultist, out var bloodstreamComponent) ||
                bloodstreamComponent.BloodSolution is null)
            {
                return false;
            }

            if (bloodstreamComponent.BloodSolution.Value.Comp.Solution.Volume < bloodCost)
            {
                _popupSystem.PopupEntity(Loc.GetString("cult-blood-boil-rune-no-blood"), user, user);
                return false;
            }

            _bloodstreamSystem.TryModifyBloodLevel(cultist, -bloodCost, bloodstreamComponent);
        }

        var projectileCount =
            (int) MathF.Round(MathHelper.Lerp(component.MinProjectiles, component.MaxProjectiles, severity));

        while (projectileCount > 0)
        {
            var target = _random.Pick(list);
            var targetCoords = xformQuery.GetComponent(target).Coordinates.Offset(_random.NextVector2(0.5f));
            var flammable = GetEntityQuery<FlammableComponent>();

            if (!flammable.TryGetComponent(target, out var fl))
                continue;

            fl.FireStacks += 1;

            _flammableSystem.Ignite(target, target);

            Shoot(
                rune,
                component,
                xform.Coordinates,
                targetCoords,
                severity);

            projectileCount--;
        }

        _audio.PlayPvs(_magic, rune, AudioParams.Default.WithMaxDistance(2f));

        return true;
    }

    private void Shoot(
        EntityUid uid,
        CultRuneBloodBoilComponent component,
        EntityCoordinates coords,
        EntityCoordinates targetCoords,
        float severity)
    {
        var mapPos = coords.ToMap(EntityManager, _xform);

        var spawnCoords = _mapManager.TryFindGridAt(mapPos, out var gridUid, out _)
            ? coords.WithEntityId(gridUid, EntityManager)
            : new(_mapManager.GetMapEntityId(mapPos.MapId), mapPos.Position);

        var ent = Spawn(component.ProjectilePrototype, spawnCoords);
        var direction = targetCoords.ToMapPos(EntityManager, _xform) - mapPos.Position;

        if (!TryComp<ProjectileComponent>(ent, out var comp))
            return;

        comp.Damage *= severity;

        _gunSystem.ShootProjectile(ent, direction, Vector2.Zero, uid, uid, component.ProjectileSpeed);
    }

    /*
   * BloodBoil rune end ----
    */

    //------------------------------------------//

    /*
    * Empower rune start ----
    */

    private void OnActiveInWorld(EntityUid uid, CultEmpowerComponent component, ActivateInWorldEvent args)
    {
        if (!component.IsRune || !TryComp<CultistComponent>(args.User, out _) ||
            !TryComp<ActorComponent>(args.User, out var actor))
            return;

        _ui.TryOpen(uid, CultEmpowerUiKey.Key, actor.PlayerSession);
    }

    private void OnUseInHand(EntityUid uid, CultEmpowerComponent component, UseInHandEvent args)
    {
        if (!TryComp<CultistComponent>(args.User, out _) || !TryComp<ActorComponent>(args.User, out var actor))
            return;

        _ui.TryOpen(uid, CultEmpowerUiKey.Key, actor.PlayerSession);
    }

    private void OnEmpowerSelected(EntityUid uid, CultEmpowerComponent component, CultEmpowerSelectedBuiMessage args)
    {
        var playerEntity = args.Session.AttachedEntity;

        if (!playerEntity.HasValue || !TryComp<CultistComponent>(playerEntity, out var comp))
            return;

        var action = CultistComponent.CultistActions.FirstOrDefault(x => x.Equals(args.ActionType));

        if (action == null)
            return;

        if (component.IsRune)
        {
            if (comp.SelectedEmpowers.Count >= component.MaxAllowedCultistActions)
            {
                _popupSystem.PopupEntity(Loc.GetString("cult-too-much-empowers"), uid);
                return;
            }

            comp.SelectedEmpowers.Add(GetNetEntity(_actionsSystem.AddAction(playerEntity.Value, action)));
            Dirty(playerEntity.Value, comp);
        }
        else if (comp.SelectedEmpowers.Count < component.MinRequiredCultistActions)
        {
            comp.SelectedEmpowers.Add(GetNetEntity(_actionsSystem.AddAction(playerEntity.Value, action)));
            Dirty(playerEntity.Value, comp);
        }
    }

    /*
    * Empower rune end ----
    */

    //------------------------------------------//

    /*
    * Helpers Start ----
     */

    private EntityUid? FindNearestTarget(EntityUid uid, List<EntityUid> targets)
    {
        if (!_entityManager.TryGetComponent<TransformComponent>(uid, out var runeTransform))
            return null;

        var range = 999f;
        EntityUid? victim = null;

        foreach (var target in targets)
        {
            if (!_entityManager.TryGetComponent<TransformComponent>(target, out var targetTransform))
                continue;

            runeTransform.Coordinates.TryDistance(_entityManager, targetTransform.Coordinates, out var newRange);

            if (newRange < range)
            {
                range = newRange;
                victim = target;
            }
        }

        return victim;
    }

    private HashSet<EntityUid> GatherCultists(EntityUid uid, float range)
    {
        var entities = _lookup.GetEntitiesInRange(uid, range, LookupFlags.Dynamic);
        entities.RemoveWhere(x => !HasComp<CultistComponent>(x) && !HasComp<ConstructComponent>(x));

        return entities;
    }

    private void SpawnRune(EntityUid uid, string? rune, bool teleportRune = false, string? label = null)
    {
        var transform = CompOrNull<TransformComponent>(uid)?.Coordinates;

        if (transform == null)
            return;

        if (rune == null)
            return;

        if (teleportRune)
        {
            var teleportRuneEntity = _entityManager.SpawnEntity(rune, transform.Value);
            _xform.AttachToGridOrMap(teleportRuneEntity);

            _entityManager.TryGetComponent<CultRuneTeleportComponent>(teleportRuneEntity, out var sex);
            {
                if (sex == null)
                    return;

                label = string.IsNullOrEmpty(label) ? Loc.GetString("cult-teleport-rune-default-label") : label;

                if (label.Length > 18)
                {
                    label = label.Substring(0, 18);
                }

                sex.Label = label;
            }

            return;
        }

        var damage = 10;

        if (rune == ApocalypseRunePrototypeId)
        {
            if (!_entityManager.TryGetComponent(uid, out TransformComponent? transComp))
            {
                return;
            }

            damage = 40;
            var pos = transComp.MapPosition;
            var x = (int) pos.X;
            var y = (int) pos.Y;
            var posText = $"(x = {x}, y = {y})";
            _chat.DispatchGlobalAnnouncement(Loc.GetString("cult-narsie-summon-drawn-position", ("posText", posText)),
                "CULT", true, _apocRuneEndDrawing, colorOverride: Color.DarkRed);
        }

        var damageSpecifier = new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>("Slash"), damage);
        _damageableSystem.TryChangeDamage(uid, damageSpecifier, true, false);

        _xform.AttachToGridOrMap(_entityManager.SpawnEntity(rune, transform.Value));
    }

    private bool SpawnShard(EntityUid target)
    {
        if (!_entityManager.TryGetComponent<MindContainerComponent>(target, out var mindComponent))
            return false;

        var transform = CompOrNull<TransformComponent>(target)?.Coordinates;

        if (transform == null)
            return false;

        if (!mindComponent.Mind.HasValue)
            return false;

        var shard = _entityManager.SpawnEntity("SoulShard", transform.Value);

        _mindSystem.TransferTo(mindComponent.Mind.Value, shard);

        _bodySystem.GibBody(target);

        return true;
    }

    private void AddChargesToReviveRune(uint amount = 1)
    {
        CultRuneReviveComponent.ChargesLeft += amount;
    }

    private bool IsAllowedToDraw(EntityUid uid)
    {
        var transform = Transform(uid);
        var gridUid = transform.GridUid;
        var tile = transform.Coordinates.GetTileRef();

        if (!gridUid.HasValue)
        {
            _popupSystem.PopupEntity(Loc.GetString("cult-cant-draw-rune"), uid, uid);
            return false;
        }

        if (!tile.HasValue)
        {
            _popupSystem.PopupEntity(Loc.GetString("cult-cant-draw-rune"), uid, uid);
            return false;
        }

        return true;
    }

    private void HealCultist(EntityUid player)
    {
        var damageSpecifier = _prototypeManager.Index<DamageGroupPrototype>("Brute");
        var damageSpecifier2 = _prototypeManager.Index<DamageGroupPrototype>("Burn");

        _damageableSystem.TryChangeDamage(player, new DamageSpecifier(damageSpecifier, -40));
        _damageableSystem.TryChangeDamage(player, new DamageSpecifier(damageSpecifier2, -40));
    }

    private void StopPulling(EntityUid target, bool checkPullable = true)
    {
        // break pulls before portal enter so we dont break shit
        if (checkPullable && TryComp<SharedPullableComponent>(target, out var pullable) && pullable.BeingPulled)
        {
            _pulling.TryStopPull(pullable);
        }

        if (TryComp<SharedPullerComponent>(target, out var pulling)
            && pulling.Pulling != null &&
            TryComp<SharedPullableComponent>(pulling.Pulling.Value, out var subjectPulling))
        {
            _pulling.TryStopPull(subjectPulling);
        }
    }

    /*
    * Helpers End ----
     */
}
