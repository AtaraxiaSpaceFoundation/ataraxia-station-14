using System.Linq;
using Content.Shared._White.BetrayalDagger;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Wizard.SpellBlade;

public abstract class SharedSpellBladeSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpellBladeComponent, SpellBladeSystemMessage>(OnMessage);
        SubscribeLocalEvent<SpellBladeComponent, ActivatableUIOpenAttemptEvent>(OnOpenAttempt);
        SubscribeLocalEvent<SpellBladeComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<SpellBladeComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.ChosenAspect == string.Empty)
        {
            args.PushMarkup("Аспект не выбран.");
            return;
        }

        var proto = _prototypeManager.Index(ent.Comp.ChosenAspect);

        args.PushMarkup($"Выбранный аспект: {proto.Name}");
    }

    private void OnOpenAttempt(Entity<SpellBladeComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (ent.Comp.ChosenAspect == string.Empty)
            return;

        _popup.PopupEntity("Аспект уже выбран.", args.User, args.User);
        args.Cancel();
    }

    private void OnMessage(Entity<SpellBladeComponent> ent, ref SpellBladeSystemMessage args)
    {
        if (ent.Comp.ChosenAspect != string.Empty)
            return;

        switch (args.ProtoId)
        {
            case "AspectFire":
                ApplyFireAspect(ent);
                break;
            case "AspectFrost":
                ApplyFrostAspect(ent);
                break;
            case "AspectLightning":
                ApplyLightningAspect(ent);
                break;
            case "AspectBluespace":
                ApplyBluespaceAspect(ent);
                break;
            case "AspectMagicMissile":
                ApplyMagicMissileAspect(ent);
                break;
            default:
                return;
        }

        ent.Comp.ChosenAspect = args.ProtoId;

        _audio.PlayPvs(ent.Comp.AspectChosenSound, ent);


        Dirty(ent);
    }

    protected virtual void ApplyFireAspect(EntityUid uid) { }

    protected virtual void ApplyFrostAspect(EntityUid uid) { }

    protected virtual void ApplyLightningAspect(EntityUid uid) { }

    private void ApplyBluespaceAspect(EntityUid uid)
    {
        var blink = EnsureComp<BlinkComponent>(uid);
        blink.Distance = 15f;
        blink.BlinkRate = 1f;
    }

    private void ApplyMagicMissileAspect(EntityUid uid)
    {
        var gun = EnsureComp<GunComponent>(uid);
        _gun.SetUseKey(gun, false);
        _gun.SetSound(uid, new SoundPathSpecifier("/Audio/Weapons/Guns/Gunshots/Magic/staff_healing.ogg"));
        _gun.SetFireRate(uid, 1.2f);
        var ammoProvider = EnsureComp<BasicEntityAmmoProviderComponent>(uid);
        ammoProvider.Proto = "ProjectileMagicMissile";
    }

    public bool IsHoldingItemWithComponent<T>(EntityUid uid) where T : Component
    {
        return _hands.EnumerateHeld(uid).Any(HasComp<T>);
    }
}
