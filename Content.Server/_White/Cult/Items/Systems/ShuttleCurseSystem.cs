using Content.Server.Chat.Systems;
using Content.Server.RoundEnd;
using Content.Server.Shuttles.Systems;
using Content.Server._White.Cult.Items.Components;
using Content.Shared.GameTicking;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared._White.Cult;
using Robust.Shared.Timing;
using CultistComponent = Content.Shared._White.Cult.Components.CultistComponent;

namespace Content.Server._White.Cult.Items.Systems;

public sealed class ShuttleCurseSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private const int MaxCurses = 3;
    private int _currentCurses = 0;
    private TimeSpan? _nextCurse = TimeSpan.Zero;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShuttleCurseComponent, UseInHandEvent>(OnUse);
        SubscribeLocalEvent<RoundEndedEvent>(OnRoundEnd);
    }

    private void OnRoundEnd(RoundEndedEvent ev)
    {
        _currentCurses = 0;
        _nextCurse = TimeSpan.Zero;
    }

    private void OnUse(EntityUid uid, ShuttleCurseComponent component, UseInHandEvent args)
    {
        if (!HasComp<CultistComponent>(args.User))
        {
            _hands.TryDrop(args.User);
            _popup.PopupEntity(Loc.GetString("shuttle-curse-not-cultist"), args.User, args.User);
            return;
        }

        if (!_roundEnd.ShuttleCalled())
        {
            _popup.PopupEntity(Loc.GetString("shuttle-curse-shuttle-not-called"), args.User, args.User);
            return;
        }

        if (_currentCurses >= MaxCurses)
        {
            _popup.PopupEntity(Loc.GetString("shuttle-curse-max-curses"), args.User, args.User);
            return;
        }

        if (_nextCurse > _gameTiming.CurTime)
        {
            _popup.PopupEntity(Loc.GetString("shuttle-curse-cooldown"), args.User, args.User);
            return;
        }

        var shuttle = _entMan.System<EmergencyShuttleSystem>();

        if (shuttle.EmergencyShuttleArrived)
        {
            _popup.PopupEntity(Loc.GetString("shuttle-curse-shuttle-arrived"), args.User, args.User);
            return;
        }

        _roundEnd.DelayCursedShuttle(component.DelayTime);
        _popup.PopupEntity(Loc.GetString("shuttle-curse-shuttle-delayed"), args.User, args.User);

        _currentCurses++;
        _nextCurse = _gameTiming.CurTime + component.Cooldown;
    }
}
