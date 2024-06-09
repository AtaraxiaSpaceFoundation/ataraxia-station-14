using System.Linq;
using System.Numerics;
using Content.Server._White.IncorporealSystem;
using Content.Server._White.Wizard.Magic.Amaterasu;
using Content.Server._White.Wizard.Magic.Other;
using Content.Server._White.Wizard.Teleport;
using Content.Server.Abilities.Mime;
using Content.Server.Administration.Commands;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Chat.Systems;
using Content.Server.Emp;
using Content.Server.EUI;
using Content.Server.Lightning;
using Content.Server.Magic;
using Content.Server.Singularity.EntitySystems;
using Content.Server.Standing;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared._White.BetrayalDagger;
using Content.Shared._White.Events;
using Content.Shared._White.Wizard;
using Content.Shared._White.Wizard.Magic;
using Content.Shared.Actions;
using Content.Shared.Cluwne;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Item;
using Content.Shared.Magic;
using Content.Shared.Maps;
using Content.Shared.Mobs.Components;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.StatusEffect;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server._White.Wizard.Magic;

public sealed class WizardSpellsSystem : EntitySystem
{
    #region Dependencies

    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly GunSystem _gunSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly LightningSystem _lightning = default!;
    [Dependency] private readonly MagicSystem _magicSystem = default!;
    [Dependency] private readonly GravityWellSystem _gravityWell = default!;
    [Dependency] private readonly FlammableSystem _flammableSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly ThrowingSystem _throwingSystem = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly EmpSystem _empSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly TelefragSystem _telefrag = default!;
    [Dependency] private readonly EuiManager _euiManager = default!;

    #endregion

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TeleportSpellEvent>(OnTeleportSpell);
        SubscribeLocalEvent<InstantRecallSpellEvent>(OnInstantRecallSpell);
        SubscribeLocalEvent<MimeTouchSpellEvent>(OnMimeTouchSpell);
        SubscribeLocalEvent<BananaTouchSpellEvent>(OnBananaTouchSpell);
        SubscribeLocalEvent<CluwneCurseSpellEvent>(OnCluwneCurseSpell);
        SubscribeLocalEvent<EmpSpellEvent>(OnEmpSpell);
        SubscribeLocalEvent<EtherealJauntSpellEvent>(OnJauntSpell);
        SubscribeLocalEvent<BlinkSpellEvent>(OnBlinkSpell);
        SubscribeLocalEvent<ForceWallSpellEvent>(OnForcewallSpell);
        SubscribeLocalEvent<CardsSpellEvent>(OnCardsSpell);
        SubscribeLocalEvent<FireballSpellEvent>(OnFireballSpell);
        SubscribeLocalEvent<ForceSpellEvent>(OnForceSpell);
        SubscribeLocalEvent<ArcSpellEvent>(OnArcSpell);

        SubscribeLocalEvent<MagicComponent, BeforeCastSpellEvent>(OnBeforeCastSpell);
    }

    #region Teleport

    private void OnTeleportSpell(TeleportSpellEvent msg)
    {
        if (!CanCast(msg))
            return;

        if (!TryComp(msg.Performer, out ActorComponent? actor))
            return;

        var eui = new TeleportSpellEui(msg.Performer);
        _euiManager.OpenEui(eui, actor.PlayerSession);
        eui.StateDirty();

        msg.Handled = true;
        Speak(msg);
    }

    #endregion

    #region Instant Recall

    private void OnInstantRecallSpell(InstantRecallSpellEvent msg)
    {
        if (!CanCast(msg))
            return;

        if (!TryComp<HandsComponent>(msg.Performer, out var handsComponent))
            return;

        if (!TryComp<InstantRecallComponent>(msg.Action, out var recallComponent))
        {
            _popupSystem.PopupEntity("Что-то поломалось!", msg.Performer, msg.Performer);
            return;
        }

        if (handsComponent.ActiveHandEntity != null)
        {
            if (HasComp<VirtualItemComponent>(handsComponent.ActiveHandEntity.Value))
            {
                _popupSystem.PopupEntity("Не могу работать с этим!", msg.Performer, msg.Performer);
                return;
            }

            recallComponent.Item = handsComponent.ActiveHandEntity.Value;
            _popupSystem.PopupEntity($"Сопряжено с {MetaData(handsComponent.ActiveHandEntity.Value).EntityName}",
                msg.Performer, msg.Performer);
            return;
        }

        if (handsComponent.ActiveHandEntity == null && recallComponent.Item != null &&
            Exists(recallComponent.Item.Value))
        {
            var coordsItem = Transform(recallComponent.Item.Value).Coordinates;
            var coordsPerformer = Transform(msg.Performer).Coordinates;

            Spawn("EffectEmpPulse", coordsItem);

            _transformSystem.SetCoordinates(recallComponent.Item.Value, coordsPerformer);
            _transformSystem.AttachToGridOrMap(recallComponent.Item.Value);

            _handsSystem.TryForcePickupAnyHand(msg.Performer, recallComponent.Item.Value);

            msg.Handled = true;
            return;
        }

        _popupSystem.PopupEntity("Нет привязки.", msg.Performer, msg.Performer);
    }

    #endregion

    #region Mime Touch

    private void OnMimeTouchSpell(MimeTouchSpellEvent msg)
    {
        if (!CanCast(msg))
            return;

        if (!HasComp<HumanoidAppearanceComponent>(msg.Target))
        {
            _popupSystem.PopupEntity("Работает только на людях!", msg.Performer, msg.Performer);
            return;
        }

        SetOutfitCommand.SetOutfit(msg.Target, "MimeGear", EntityManager);
        var powers = EnsureComp<MimePowersComponent>(msg.Target);
        powers.CanBreakVow = false;

        Spawn("AdminInstantEffectSmoke3", Transform(msg.Target).Coordinates);

        msg.Handled = true;
        Speak(msg);
    }

    #endregion

    #region Banana Touch

    private void OnBananaTouchSpell(BananaTouchSpellEvent msg)
    {
        if (!CanCast(msg))
            return;

        if (!HasComp<HumanoidAppearanceComponent>(msg.Target))
        {
            _popupSystem.PopupEntity("Работает только на людях!", msg.Performer, msg.Performer);
            return;
        }

        SetOutfitCommand.SetOutfit(msg.Target, "ClownGear", EntityManager);
        EnsureComp<ClumsyComponent>(msg.Target);

        Spawn("AdminInstantEffectSmoke3", Transform(msg.Target).Coordinates);

        msg.Handled = true;
        Speak(msg);
    }

    #endregion

    #region Cluwne Curse

    private void OnCluwneCurseSpell(CluwneCurseSpellEvent msg)
    {
        if (!CanCast(msg))
            return;

        if (!HasComp<HumanoidAppearanceComponent>(msg.Target))
        {
            _popupSystem.PopupEntity("Работает только на людях!", msg.Performer, msg.Performer);
            return;
        }

        EnsureComp<CluwneComponent>(msg.Target);

        Spawn("AdminInstantEffectSmoke3", Transform(msg.Target).Coordinates);

        msg.Handled = true;
        Speak(msg);
    }

    #endregion

    #region EMP

    private void OnEmpSpell(EmpSpellEvent msg)
    {
        if (!CanCast(msg))
            return;

        var coords = _transformSystem.ToMapCoordinates(Transform(msg.Performer).Coordinates);

        _empSystem.EmpPulse(coords, 15, 1000000, 60f);

        msg.Handled = true;
        Speak(msg);
    }

    #endregion

    #region Ethereal Jaunt

    private void OnJauntSpell(EtherealJauntSpellEvent msg)
    {
        if (!CanCast(msg))
            return;

        if (_statusEffectsSystem.HasStatusEffect(msg.Performer, "Incorporeal"))
        {
            _popupSystem.PopupEntity("Вы уже в потустороннем мире", msg.Performer, msg.Performer);
            return;
        }

        Spawn("AdminInstantEffectSmoke10", Transform(msg.Performer).Coordinates);

        _statusEffectsSystem.TryAddStatusEffect<IncorporealComponent>(msg.Performer, "Incorporeal",
            TimeSpan.FromSeconds(10), false);

        msg.Handled = true;
        Speak(msg);
    }

    #endregion

    #region Blink

    private void OnBlinkSpell(BlinkSpellEvent msg)
    {
        if (!CanCast(msg))
            return;

        var transform = Transform(msg.Performer);

        var oldCoords = transform.Coordinates;

        EntityCoordinates coords = default;
        var foundTeleportPos = false;
        var attempts = 10;

        while (attempts > 0)
        {
            attempts--;

            var random = new Random().Next(10, 20);
            var offset = transform.LocalRotation.ToWorldVec().Normalized();
            var direction = transform.LocalRotation.GetDir().ToVec();
            var newOffset = offset + direction * random;
            coords = transform.Coordinates.Offset(newOffset).SnapToGrid(EntityManager);

            var tile = coords.GetTileRef();

            if (tile != null && _turf.IsTileBlocked(tile.Value, CollisionGroup.AllMask))
                continue;

            foundTeleportPos = true;
            break;
        }

        if (!foundTeleportPos)
            return;

        _telefrag.Telefrag(coords, msg.Performer);
        _transformSystem.SetCoordinates(msg.Performer, coords);
        _transformSystem.AttachToGridOrMap(msg.Performer);

        _audio.PlayPvs("/Audio/White/Cult/veilin.ogg", coords);
        _audio.PlayPvs("/Audio/White/Cult/veilout.ogg", oldCoords);

        Spawn("AdminInstantEffectSmoke3", oldCoords);
        Spawn("AdminInstantEffectSmoke3", coords);

        msg.Handled = true;
        Speak(msg);
    }

    #endregion

    #region Forcewall

    private void OnForcewallSpell(ForceWallSpellEvent msg)
    {
        if (!CanCast(msg))
            return;

        switch (msg.ActionUseType)
        {
            case ActionUseType.Default:
                ForcewallSpellDefault(msg);
                break;
            case ActionUseType.Charge:
                ForcewallSpellCharge(msg);
                break;
            case ActionUseType.AltUse:
                ForcewallSpellAlt(msg);
                break;
        }

        SetCooldown(msg.Action, msg.ActionUseType);
        msg.Handled = true;
        Speak(msg);
    }

    private void ForcewallSpellDefault(ForceWallSpellEvent msg)
    {
        var transform = Transform(msg.Performer);

        foreach (var position in _magicSystem.GetPositionsInFront(transform))
        {
            var ent = Spawn(msg.Prototype, position.SnapToGrid(EntityManager, _mapManager));

            var comp = EnsureComp<PreventCollideComponent>(ent);
            comp.Uid = msg.Performer;
        }
    }

    private void ForcewallSpellCharge(ForceWallSpellEvent msg)
    {
        var xform = Transform(msg.Performer);

        var positions = GetArenaPositions(xform.Coordinates, msg.ChargeLevel);

        foreach (var position in positions)
        {
            var ent = Spawn(msg.Prototype, position);

            var comp = EnsureComp<PreventCollideComponent>(ent);
            comp.Uid = msg.Performer;
        }
    }

    private void ForcewallSpellAlt(ForceWallSpellEvent msg)
    {
        var positions = GetArenaPositions(msg.Target, 2);

        foreach (var direction in positions)
        {
            var ent = Spawn(msg.Prototype, direction);

            var comp = EnsureComp<PreventCollideComponent>(ent);
            comp.Uid = msg.Performer;
        }
    }

    #endregion

    #region Cards

    private void OnCardsSpell(CardsSpellEvent msg)
    {
        if (!CanCast(msg))
            return;

        var result = true;

        switch (msg.ActionUseType)
        {
            case ActionUseType.Default:
                CardsSpellDefault(msg);
                break;
            case ActionUseType.Charge:
                CardsSpellCharge(msg);
                break;
            case ActionUseType.AltUse:
                result = CardsSpellAlt(msg);
                break;
        }

        if (!result)
            return;

        SetCooldown(msg.Action, msg.ActionUseType);
        msg.Handled = true;
        Speak(msg);
    }

    private void CardsSpellDefault(CardsSpellEvent msg)
    {
        TurnOffShield(msg.Performer);

        var xform = Transform(msg.Performer);

        for (var i = 0; i < 10; i++)
        {
            foreach (var pos in _magicSystem.GetSpawnPositions(xform, msg.Pos))
            {
                var mapPos = _transformSystem.ToMapCoordinates(pos);
                var spawnCoords = _mapManager.TryFindGridAt(mapPos, out var gridUid, out _)
                    ? pos.WithEntityId(gridUid, EntityManager)
                    : new EntityCoordinates(_mapManager.GetMapEntityId(mapPos.MapId), mapPos.Position);

                var ent = Spawn(msg.Prototype, spawnCoords);

                var direction = msg.Target.ToMapPos(EntityManager, _transformSystem) -
                                spawnCoords.ToMapPos(EntityManager, _transformSystem);
                var randomizedDirection = direction + new Vector2(_random.Next(-2, 2), _random.Next(-2, 2));

                _throwingSystem.TryThrow(ent, randomizedDirection, 60, msg.Performer);
            }
        }
    }

    private void CardsSpellCharge(CardsSpellEvent msg)
    {
        TurnOffShield(msg.Performer);

        var xform = Transform(msg.Performer);

        var count = 10 + 10 * msg.ChargeLevel;
        var angleStep = 360f / count;

        for (var i = 0; i < count; i++)
        {
            var angle = i * angleStep;

            var direction = new Vector2(MathF.Cos(MathHelper.DegreesToRadians(angle)),
                MathF.Sin(MathHelper.DegreesToRadians(angle)));

            foreach (var pos in _magicSystem.GetSpawnPositions(xform, msg.Pos))
            {
                var mapPos = _transformSystem.ToMapCoordinates(pos);

                var spawnCoords = _mapManager.TryFindGridAt(mapPos, out var gridUid, out _)
                    ? pos.WithEntityId(gridUid, EntityManager)
                    : new EntityCoordinates(_mapManager.GetMapEntityId(mapPos.MapId), mapPos.Position);

                var ent = Spawn(msg.Prototype, spawnCoords);

                _throwingSystem.TryThrow(ent, direction, 60, msg.Performer);
            }
        }
    }

    private bool CardsSpellAlt(CardsSpellEvent msg)
    {
        if (!HasComp<ItemComponent>(msg.TargetUid))
        {
            _popupSystem.PopupEntity("В карту можно превратить только предметы.", msg.Performer, msg.Performer);
            return false;
        }

        Del(msg.TargetUid);
        var item = Spawn(msg.Prototype);
        _handsSystem.TryPickupAnyHand(msg.Performer, item);
        TurnOffShield(msg.Performer);

        return true;
    }

    #endregion

    #region Fireball

    private void OnFireballSpell(FireballSpellEvent msg)
    {
        if (!CanCast(msg))
            return;

        var result = true;

        switch (msg.ActionUseType)
        {
            case ActionUseType.Default:
                FireballSpellDefault(msg);
                break;
            case ActionUseType.Charge:
                FireballSpellCharge(msg);
                break;
            case ActionUseType.AltUse:
                result = FireballSpellAlt(msg);
                break;
        }

        if (!result)
            return;

        SetCooldown(msg.Action, msg.ActionUseType);
        msg.Handled = true;
        Speak(msg);
    }

    private void FireballSpellDefault(FireballSpellEvent msg)
    {
        TurnOffShield(msg.Performer);

        var xform = Transform(msg.Performer);

        foreach (var pos in _magicSystem.GetSpawnPositions(xform, msg.Pos))
        {
            var mapPos = _transformSystem.ToMapCoordinates(pos);
            var spawnCoords = _mapManager.TryFindGridAt(mapPos, out var gridUid, out var grid)
                ? pos.WithEntityId(gridUid, EntityManager)
                : new EntityCoordinates(_mapManager.GetMapEntityId(mapPos.MapId), mapPos.Position);

            var userVelocity = Vector2.Zero;

            if (grid != null && TryComp(gridUid, out PhysicsComponent? physics))
                userVelocity = physics.LinearVelocity;

            var ent = Spawn(msg.Prototype, spawnCoords);
            var direction = msg.Target.ToMapPos(EntityManager, _transformSystem) -
                            spawnCoords.ToMapPos(EntityManager, _transformSystem);
            _gunSystem.ShootProjectile(ent, direction, userVelocity, msg.Performer, msg.Performer);
        }
    }

    private void FireballSpellCharge(FireballSpellEvent msg)
    {
        var coords = Transform(msg.Performer).Coordinates;

        var targets = _lookup.GetEntitiesInRange<FlammableComponent>(coords, 2 * msg.ChargeLevel);

        foreach (var target in targets.Where(target => target.Owner != msg.Performer))
        {
            target.Comp.FireStacks += 2 + msg.ChargeLevel * 2;
            _flammableSystem.Ignite(target, msg.Performer);
        }
    }

    private bool FireballSpellAlt(FireballSpellEvent msg)
    {
        if (!TryComp<FlammableComponent>(msg.TargetUid, out var flammableComponent))
        {
            _popupSystem.PopupEntity("Это нельзя поджечь!", msg.Performer, msg.Performer);
            return false;
        }

        flammableComponent.FireStacks += 4;

        _flammableSystem.Ignite(msg.TargetUid, msg.Performer);

        EnsureComp<AmaterasuComponent>(msg.TargetUid);

        return true;
    }

    #endregion

    #region Force

    private void OnForceSpell(ForceSpellEvent msg)
    {
        if (!CanCast(msg))
            return;

        var result = true;

        switch (msg.ActionUseType)
        {
            case ActionUseType.Default:
                ForceSpellDefault(msg);
                break;
            case ActionUseType.Charge:
                ForceSpellCharge(msg);
                break;
            case ActionUseType.AltUse:
                result = ForceSpellAlt(msg);
                break;
        }

        if (!result)
            return;

        SetCooldown(msg.Action, msg.ActionUseType);
        msg.Handled = true;
        Speak(msg);
    }

    private bool ForceSpellAlt(ForceSpellEvent msg)
    {
        if (!HasComp<TransformComponent>(msg.TargetUid) || !HasComp<PhysicsComponent>(msg.TargetUid))
        {
            _popupSystem.PopupEntity("Невозможно это притянуть!", msg.Performer, msg.Performer);
            return false;
        }

        _throwingSystem.TryThrow(msg.TargetUid, Transform(msg.Performer).Coordinates, 5f);
        _standing.TryLieDown(msg.TargetUid);

        return true;
    }

    private void ForceSpellCharge(ForceSpellEvent msg)
    {
        _gravityWell.GravPulse(msg.Performer, 15, 0, -80 * msg.ChargeLevel, -2 * msg.ChargeLevel, null, msg.ChargeLevel,
            new() {msg.Performer});
    }

    private void ForceSpellDefault(ForceSpellEvent msg)
    {
        _gravityWell.GravPulse(msg.Target, 10, 0, 200, 10, 2f, new() {msg.Performer});
    }

    #endregion

    #region Arc

    private void OnArcSpell(ArcSpellEvent msg)
    {
        if (!CanCast(msg))
            return;

        var result = true;

        switch (msg.ActionUseType)
        {
            case ActionUseType.Default:
                result = ArcSpellDefault(msg);
                break;
            case ActionUseType.Charge:
                ArcSpellCharge(msg);
                break;
            case ActionUseType.AltUse:
                ArcSpellAlt(msg);
                break;
        }

        if (!result)
            return;

        SetCooldown(msg.Action, msg.ActionUseType);
        msg.Handled = true;
        Speak(msg);
    }

    private bool ArcSpellDefault(ArcSpellEvent msg)
    {
        const int possibleEntitiesCount = 2;

        var entitiesInRange = _lookup.GetEntitiesInRange(msg.Target, 1);
        var entitiesToHit = entitiesInRange.Where(HasComp<MobStateComponent>).Take(possibleEntitiesCount);

        var entityUids = entitiesToHit.ToList();
        foreach (var entity in entityUids)
        {
            _lightning.ShootLightning(msg.Performer, entity, "WizardLightning", true, msg.Performer);
        }

        return entityUids.Count != 0;
    }

    private void ArcSpellCharge(ArcSpellEvent msg)
    {
        _lightning.ShootRandomLightnings(msg.Performer, 1.5f * msg.ChargeLevel, msg.ChargeLevel * 2, "WizardLightning", 2,
            caster: msg.Performer);
    }

    private void ArcSpellAlt(ArcSpellEvent msg)
    {
        TurnOffShield(msg.Performer);

        var xform = Transform(msg.Performer);

        foreach (var pos in _magicSystem.GetSpawnPositions(xform, msg.Pos))
        {
            var mapPos = _transformSystem.ToMapCoordinates(pos);
            var spawnCoords = _mapManager.TryFindGridAt(mapPos, out var gridUid, out var grid)
                ? pos.WithEntityId(gridUid, EntityManager)
                : new EntityCoordinates(_mapManager.GetMapEntityId(mapPos.MapId), mapPos.Position);

            var userVelocity = Vector2.Zero;

            if (grid != null && TryComp(gridUid, out PhysicsComponent? physics))
                userVelocity = physics.LinearVelocity;

            var ent = Spawn(msg.Prototype, spawnCoords);
            var direction = msg.Target.ToMapPos(EntityManager, _transformSystem) -
                            spawnCoords.ToMapPos(EntityManager, _transformSystem);
            _gunSystem.ShootProjectile(ent, direction, userVelocity, msg.Performer, msg.Performer);
        }
    }

    #endregion

    #region Helpers

    private void TurnOffShield(EntityUid uid)
    {
        RaiseLocalEvent(uid, new EnergyDomeClothesTurnOffEvent());
    }

    private bool CanCast(BaseActionEvent msg)
    {
        return !msg.Handled && CheckRequirements(msg.Action, msg.Performer) &&
               !_statusEffectsSystem.HasStatusEffect(msg.Performer, "Incorporeal");
    }

    private void Speak(BaseActionEvent args)
    {
        if (args is not ISpeakSpell speak || string.IsNullOrWhiteSpace(speak.Speech))
            return;

        _chat.TrySendInGameICMessage(args.Performer, Loc.GetString(speak.Speech),
            InGameICChatType.Speak, false);
    }

    private void SetCooldown(EntityUid action, ActionUseType useType)
    {
        if (!TryComp(action, out VariableUseDelayComponent? variableUseDelayComponent))
            return;

        var cooldown = useType switch
        {
            ActionUseType.Default => variableUseDelayComponent.UseDelay,
            ActionUseType.AltUse => variableUseDelayComponent.AltUseDelay,
            ActionUseType.Charge => variableUseDelayComponent.ChargeUseDelay,
            _ => TimeSpan.FromSeconds(60)
        };

        _actions.SetUseDelay(action, cooldown);
    }

    private List<EntityCoordinates> GetArenaPositions(EntityCoordinates coords, int arenaSize)
    {
        var positions = new List<EntityCoordinates>();

        arenaSize--;

        for (var i = -arenaSize; i <= arenaSize; i++)
        {
            for (var j = -arenaSize; j <= arenaSize; j++)
            {
                var position = new Vector2(i, j);
                var coordinates = coords.Offset(position);
                positions.Add(coordinates);
            }
        }

        return positions;
    }

    private bool CheckRequirements(EntityUid spell, EntityUid performer)
    {
        var ev = new BeforeCastSpellEvent(performer);
        RaiseLocalEvent(spell, ref ev);
        return !ev.Cancelled;
    }

    private void OnBeforeCastSpell(Entity<MagicComponent> ent, ref BeforeCastSpellEvent args)
    {
        var comp = ent.Comp;
        var hasReqs = false;

        if (comp.RequiresClothes)
        {
            var enumerator = _inventory.GetSlotEnumerator(args.Performer, SlotFlags.OUTERCLOTHING | SlotFlags.HEAD);
            while (enumerator.MoveNext(out var containerSlot))
            {
                if (containerSlot.ContainedEntity is { } item)
                    hasReqs = HasComp<WizardClothesComponent>(item);
                else
                    hasReqs = false;

                if (!hasReqs)
                    break;
            }
        }

        if (!hasReqs)
        {
            args.Cancelled = true;
            _popupSystem.PopupEntity(Loc.GetString("magic-component-missing-req"), args.Performer, args.Performer);
        }
    }

    #endregion
}
