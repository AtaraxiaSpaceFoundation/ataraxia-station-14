using System.Diagnostics;
using Content.Server._White.Cult.Structures;
using Content.Shared._White.Keyhole.Components;
using Content.Shared._White.Keyhole;
using Content.Shared.DoAfter;
using Content.Shared.Doors.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

namespace Content.Server._White.Keyhole;

public sealed class KeyholeSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<KeyComponent, ComponentInit>(OnKeyInit);

        SubscribeLocalEvent<KeyComponent, AfterInteractEvent>(OnKeyInsert);
        SubscribeLocalEvent<KeyholeComponent, KeyInsertDoAfterEvent>(OnDoAfter);
    }

    private void OnKeyInit(EntityUid uid, KeyComponent component, ComponentInit ev)
    {
        component.FormId ??= _random.Next(1000);
    }

    private void OnKeyInsert(EntityUid uid, KeyComponent component, AfterInteractEvent ev)
    {
        if (!ev.Target.HasValue)
        {
            return;
        }

        Debug.Assert(component.FormId != null);

        if (TryComp<KeyformComponent>(ev.Target, out var keyformComponent))
        {
            OnKeyInsertForm(uid, component, keyformComponent, ev.Target.Value, ev.User);
            return;
        }

        if (!TryComp<KeyholeComponent>(ev.Target, out var keyholeComponent) || !CanLock(ev.Target.Value))
            return;

        if (keyholeComponent.FormId != null && keyholeComponent.FormId != component.FormId)
        {
            _popupSystem.PopupEntity(Loc.GetString("door-keyhole-different-form"), ev.Target.Value);
            return;
        }

        var doAfterEventArgs =
            new DoAfterArgs(EntityManager, ev.User, keyholeComponent.Delay,
                new KeyInsertDoAfterEvent(component.FormId.Value), ev.Target, ev.Used)
            {
                BreakOnMove = true,
                BreakOnDamage = true
            };

        _doAfter.TryStartDoAfter(doAfterEventArgs);
    }

    private bool CanLock(EntityUid uid)
    {
        return !HasComp<RunicDoorComponent>(uid) && TryComp<DoorComponent>(uid, out var doorComponent) &&
               doorComponent.State == DoorState.Closed;
    }

    private void OnDoAfter(EntityUid uid, KeyholeComponent component, KeyInsertDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || !CanLock(uid))
            return;

        Debug.Assert(component.FormId == null || component.FormId == args.FormId);

        component.FormId = args.FormId;

        Lock(uid, component, args.User);

        args.Handled = true;
    }

    private void Lock(EntityUid uid, KeyholeComponent component, EntityUid user)
    {
        var sound = component.Locked ? component.UnlockSound : component.LockSound;
        var message = Loc.GetString(component.Locked ? "key-unlock-message" : "key-lock-message", ("name", user),
            ("door", uid));

        var audioParams = new AudioParams().WithVolume(-5f);

        _audio.PlayPvs(sound, user, audioParams);
        _popupSystem.PopupEntity(message, uid);

        component.Locked = !component.Locked;
    }

    private void OnKeyInsertForm(EntityUid uid, KeyComponent keyComponent, KeyformComponent keyformComponent, EntityUid keyform, EntityUid user)
    {
        if (!keyformComponent.IsUsed)
        {
            keyformComponent.FormId ??= keyComponent.FormId;
            _appearance.SetData(keyform, KeyformVisuals.IsUsed, true);

            _audio.PlayPvs(keyformComponent.PressSound, uid);
            _popupSystem.PopupEntity(Loc.GetString("key-pressed-in-keyform-message-first", ("user", user), ("key", uid)), uid);

            keyformComponent.IsUsed = true;
        }
        else
        {
            keyComponent.FormId = keyformComponent.FormId;
            _popupSystem.PopupEntity(Loc.GetString("key-pressed-in-keyform-message", ("user", user), ("key", uid)), uid);
        }
    }
}