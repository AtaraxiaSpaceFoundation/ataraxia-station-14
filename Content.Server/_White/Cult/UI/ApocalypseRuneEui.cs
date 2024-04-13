using Content.Server.Chat.Systems;
using Content.Server.DoAfter;
using Content.Server.EUI;
using Content.Shared._White.Cult.Runes;
using Content.Shared._White.Cult.UI;
using Content.Shared.DoAfter;
using Content.Shared.Eui;
using Robust.Server.Audio;
using Robust.Shared.Audio;

namespace Content.Server._White.Cult.UI;

public sealed class ApocalypseRuneEui : BaseEui
{
    private readonly SoundPathSpecifier _apocRuneStartDrawing = new("/Audio/White/Cult/startdraw.ogg");
    private const string ApocalypseRunePrototypeId = "ApocalypseRune";

    private readonly EntityUid _whoCalled;
    private readonly IEntityManager _entityManager;

    public ApocalypseRuneEui(EntityUid whoCalled, IEntityManager entityManager)
    {
        _whoCalled = whoCalled;
        _entityManager = entityManager;
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is not ApocalypseRuneDrawMessage {Accepted: true})
        {
            Close();
            return;
        }

        var ev = new CultDrawEvent
        {
            Rune = ApocalypseRunePrototypeId
        };

        var argsDoAfterEvent = new DoAfterArgs(_entityManager, _whoCalled, 120f, ev, _whoCalled)
        {
            BreakOnMove = true,
            NeedHand = true
        };

        if (!_entityManager.System<DoAfterSystem>().TryStartDoAfter(argsDoAfterEvent))
        {
            Close();
            return;
        }

        _entityManager.System<ChatSystem>().DispatchGlobalAnnouncement(Loc.GetString("cult-started-drawing-rune-end"),
            "CULT", true, _apocRuneStartDrawing, colorOverride: Color.DarkRed);

        _entityManager.System<AudioSystem>().PlayPvs("/Audio/White/Cult/butcher.ogg", _whoCalled,
            AudioParams.Default.WithMaxDistance(2f));
        Close();
    }
}
