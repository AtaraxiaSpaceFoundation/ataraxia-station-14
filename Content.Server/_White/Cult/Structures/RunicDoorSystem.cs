using Content.Server.Doors.Systems;
using Content.Shared._White.Chaplain;
using Content.Shared.Doors;
using Content.Shared.Humanoid;
using Content.Shared.Stunnable;
using Content.Shared._White.Cult;
using Content.Shared.Doors.Components;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Systems;
using CultistComponent = Content.Shared._White.Cult.Components.CultistComponent;

namespace Content.Server._White.Cult.Structures;

public sealed class RunicDoorSystem : EntitySystem
{
    [Dependency] private readonly DoorSystem _doorSystem = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RunicDoorComponent, BeforeDoorOpenedEvent>(OnBeforeDoorOpened);
        SubscribeLocalEvent<RunicDoorComponent, BeforeDoorClosedEvent>(OnBeforeDoorClosed);
        SubscribeLocalEvent<RunicDoorComponent, AttackedEvent>(OnGetAttacked);
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

        _doorSystem.Deny(airlock);

        if (!HasComp<HumanoidAppearanceComponent>(user) || HasComp<HolyComponent>(user))
            return false;

        var direction = Transform(user).MapPosition.Position - Transform(airlock).MapPosition.Position;
        var impulseVector = direction * 2000;

        _physics.ApplyLinearImpulse(user, impulseVector);

        _stunSystem.TryParalyze(user, TimeSpan.FromSeconds(3), true);
        return false;
    }
}
