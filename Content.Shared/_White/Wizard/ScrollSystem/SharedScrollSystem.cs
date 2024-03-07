using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;

namespace Content.Shared._White.Wizard.ScrollSystem;

public abstract class SharedScrollSystem : EntitySystem
{
    #region Dependencies

    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    #endregion

    #region Init

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ScrollComponent, UseInHandEvent>(OnScrollUse);
        SubscribeLocalEvent<ScrollComponent, ScrollDoAfterEvent>(OnScrollDoAfter);
    }

    #endregion

    #region Handlers

    private void OnScrollUse(EntityUid uid, ScrollComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, component.LearnTime, new ScrollDoAfterEvent(), uid, target: uid)
        {
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            BreakOnDamage = true,
            NeedHand = true
        };

        if (_net.IsServer)
        {
            _audioSystem.PlayPvs(component.UseSound, args.User);
        }

        _popupSystem.PopupClient($"You start learning about {component.LearnPopup}.", args.User, args.User, PopupType.Medium);

        _doAfterSystem.TryStartDoAfter(doAfterEventArgs);

        args.Handled = true;
    }

    private void OnScrollDoAfter(EntityUid uid, ScrollComponent component, ScrollDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        _actionsSystem.AddAction(args.User, component.ActionId);

        if (_net.IsServer)
        {
            _audioSystem.PlayEntity(component.AfterUseSound, args.User, args.User);
        }

        _popupSystem.PopupClient($"You learned much about {component.LearnPopup}. The scroll is slowly burning in your hands.", args.User, args.User, PopupType.Medium);

        BurnScroll(uid);

        args.Handled = true;
    }

    #endregion

    #region Helpers

    protected virtual void BurnScroll(EntityUid uid) {}

    #endregion
}
