using Content.Shared._White.Chaplain;
using Content.Shared.Doors;
using Content.Shared.Humanoid;
using Content.Shared.Stunnable;
using Content.Shared._White.Cult.Components;
using Content.Shared._White.Cult.Systems;
using Content.Shared.Cuffs;
using Content.Shared.Cuffs.Components;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Mobs.Systems;
using Content.Shared.Prying.Components;
using Content.Shared.Weapons.Melee.Components;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using CultistComponent = Content.Shared._White.Cult.Components.CultistComponent;

namespace Content.Shared._White.Cult.Structures;

public sealed class RunicDoorSystem : EntitySystem
{
    [Dependency] private readonly SharedDoorSystem _doorSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly OccluderSystem _occluder = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedCuffableSystem _cuffable = default!;
    [Dependency] private readonly HolyWeaponSystem _holyWeapon = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RunicDoorComponent, BeforeDoorOpenedEvent>(OnBeforeDoorOpened);
        SubscribeLocalEvent<RunicDoorComponent, BeforeDoorClosedEvent>(OnBeforeDoorClosed);
        // SubscribeLocalEvent<RunicDoorComponent, AttackedEvent>(OnGetAttacked);
        SubscribeLocalEvent<RunicDoorComponent, ConcealEvent>(OnConceal);
        SubscribeLocalEvent<RunicDoorComponent, BeforePryEvent>(OnBeforePry);
    }

    private void OnBeforePry(Entity<RunicDoorComponent> ent, ref BeforePryEvent args)
    {
        args.Cancelled = true;
    }

    private void OnConceal(Entity<RunicDoorComponent> ent, ref ConcealEvent args)
    {
        if (!TryComp(ent, out MetaDataComponent? meta))
            return;

        if (TryComp(ent, out PhysicsComponent? physics))
            _occluder.SetEnabled(ent, args.Conceal && physics.CanCollide, meta: meta);

        if (TryComp(ent, out DoorComponent? door))
        {
            door.Occludes = args.Conceal;
            Dirty(ent, door, meta);
        }

        if (!TryComp(ent, out MeleeSoundComponent? meleeSound) || meleeSound.SoundGroups == null)
            return;

        meleeSound.SoundGroups["Brute"] = args.Conceal
            ? new SoundPathSpecifier("/Audio/Weapons/smash.ogg")
            : new SoundCollectionSpecifier("GlassSmash");

        Dirty(ent, meleeSound, meta);
    }

    private void OnGetAttacked(Entity<RunicDoorComponent> ent, ref AttackedEvent args)
    {
        if (!HasComp<HolyWeaponComponent>(args.Used) || !TryComp<DoorComponent>(ent, out var doorComp) ||
            doorComp.State is not DoorState.Closed)
            return;

        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Magic/knock.ogg"), ent);

        _doorSystem.StartOpening(ent, doorComp);
    }

    private void OnBeforeDoorOpened(EntityUid uid, RunicDoorComponent component, BeforeDoorOpenedEvent args)
    {
        args.Uncancel();

        if (!args.User.HasValue)
        {
            return;
        }

        if (!Process(uid, args.User.Value))
        {
            args.Cancel();
        }
    }

    private void OnBeforeDoorClosed(EntityUid uid, RunicDoorComponent component, BeforeDoorClosedEvent args)
    {
        args.Uncancel();

        if (!args.User.HasValue)
        {
            return;
        }

        if (!Process(uid, args.User.Value))
        {
            args.Cancel();
        }
    }

    private bool Process(EntityUid airlock, EntityUid user)
    {
        if (HasComp<CultistComponent>(user) || HasComp<ConstructComponent>(user))
        {
            return true;
        }

        // _doorSystem.Deny(airlock);

        if (!HasComp<HumanoidAppearanceComponent>(user) || _holyWeapon.IsHoldingHolyWeapon(user) ||
            TryComp(airlock, out ConcealableComponent? concealable) && concealable.Concealed)
            return false;

        var direction = _transform.GetMapCoordinates(user).Position - _transform.GetMapCoordinates(airlock).Position;
        var impulseVector = direction * 2000;

        _physics.ApplyLinearImpulse(user, impulseVector);

        _stunSystem.TryParalyze(user, TimeSpan.FromSeconds(3), true);
        return false;
    }

    public bool CanBumpOpen(EntityUid uid, EntityUid otherUid)
    {
        return !HasComp<RunicDoorComponent>(uid) || !HasComp<ConstructComponent>(otherUid) &&
            (!HasComp<CultistComponent>(otherUid) || !_mobState.IsAlive(otherUid) ||
             TryComp(otherUid, out CuffableComponent? cuffable) && _cuffable.GetAllCuffs(cuffable).Count > 0);
    }
}
