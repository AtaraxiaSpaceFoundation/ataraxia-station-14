using Content.Client.Eui;
using Content.Client.Ghost.UI;
using Content.Shared._White.Cult.UI;
using JetBrains.Annotations;
using Robust.Client.Graphics;

namespace Content.Client._White.Cult.UI.ApocalypseRuneEui;

[UsedImplicitly]
public sealed class ApocalypseRuneEui : BaseEui
{
    private readonly ApocalypseRuneMenu _menu;

    public ApocalypseRuneEui()
    {
        _menu = new ApocalypseRuneMenu();

        _menu.DenyButton.OnPressed += _ =>
        {
            SendMessage(new ApocalypseRuneDrawMessage(false));
            _menu.Close();
        };

        _menu.AcceptButton.OnPressed += _ =>
        {
            SendMessage(new ApocalypseRuneDrawMessage(true));
            _menu.Close();
        };
    }

    public override void Opened()
    {
        IoCManager.Resolve<IClyde>().RequestWindowAttention();
        _menu.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();

        SendMessage(new ApocalypseRuneDrawMessage(false));
        _menu.Close();
    }

}
