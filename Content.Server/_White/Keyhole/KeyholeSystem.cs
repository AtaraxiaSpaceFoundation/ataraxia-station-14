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

// TODO: Исправить, что дверь на замке можно разобрать через ее id: DoorGraph
// TODO: Исправить, что при прерывании закрытия девственной двери форма замка принимает форму ключа, хотя закрытия не произошло

public sealed partial class KeyholeSystem : EntitySystem
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
        component.FormId = _random.Next(1000);
    }

    private void OnKeyInsert(EntityUid uid, KeyComponent component, AfterInteractEvent ev)
    {
        if (TryComp<KeyformComponent>(ev.Target, out var keyformComponent))
            OnKeyInsertForm(uid, component, keyformComponent, ev);

        if (!TryComp<KeyholeComponent>(ev.Target, out var keyholeComponent))
            return;

        keyholeComponent.FormId ??= component.FormId;

        if (!CanLock(keyholeComponent.Owner, keyholeComponent, component))
            return;

        var doAfterEventArgs =
                new DoAfterArgs(EntityManager, ev.User, keyholeComponent.Delay, new KeyInsertDoAfterEvent(), ev.Target, ev.Used)
                {
                    BreakOnTargetMove = true,
                    BreakOnUserMove = true,
                    BreakOnDamage = true
                };
        _doAfter.TryStartDoAfter(doAfterEventArgs);
    }

    private bool CanLock(EntityUid uid, KeyholeComponent keyholeComponent, KeyComponent keyComponent)
    {
        var can = TryComp<DoorComponent>(uid, out var doorComponent) &&
                  keyholeComponent.FormId == keyComponent.FormId &&
                  doorComponent.State == DoorState.Closed;

        return can;
    }

    private void OnDoAfter(EntityUid uid, KeyholeComponent component, KeyInsertDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || IsStateChanging(uid))
            return;

        Lock(uid, component, args.User);

        args.Handled = true;
    }

    private bool IsStateChanging(EntityUid uid)
    {
        return TryComp<DoorComponent>(uid, out var doorComponent) &&
               (doorComponent.State == DoorState.Closing || doorComponent.State == DoorState.Opening);
    }

    private void Lock(EntityUid uid, KeyholeComponent component, EntityUid user)
    {
        var sound = component.Locked ? component.UnlockSound : component.LockSound;
        var message = Loc.GetString(component.Locked ? "key-unlock-message" : "key-lock-message", ("name", user), ("door", uid));

        var audioParams = new AudioParams().WithVolume(-5f);

        _audio.PlayPvs(sound, user, audioParams);
        _popupSystem.PopupEntity(message, uid);

        component.Locked = !component.Locked;
    }

    private void OnKeyInsertForm(EntityUid uid, KeyComponent keyComponent, KeyformComponent keyformComponent, AfterInteractEvent args)
    {
        if (!keyformComponent.IsUsed)
        {
            keyformComponent.FormId ??= keyComponent.FormId;
            _appearance.SetData(keyformComponent.Owner, KeyformVisuals.IsUsed, true);

            _audio.PlayPvs(keyformComponent.PressSound, uid);
            _popupSystem.PopupEntity(Loc.GetString("key-pressed-in-keyform-message-first", ("user", args.User), ("key", uid)), uid);

            keyformComponent.IsUsed = true;
        }
        else
        {
            keyComponent.FormId = keyformComponent.FormId;
            _popupSystem.PopupEntity(Loc.GetString("key-pressed-in-keyform-message", ("user", args.User), ("key", uid)), uid);
        }

    }

}
