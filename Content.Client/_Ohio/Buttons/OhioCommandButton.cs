using Robust.Client.Console;

namespace Content.Client._Ohio.Buttons;

[Virtual]
public class OhioCommandButton : OhioLobbyTextButton
{
    public string? Command { get; set; }

    public OhioCommandButton()
    {
        OnPressed += Execute;
    }

    private bool CanPress()
    {
        return string.IsNullOrEmpty(Command) ||
               IoCManager.Resolve<IClientConGroupController>().CanCommand(Command.Split(' ')[0]);
    }

    protected override void EnteredTree()
    {
        if (!CanPress())
        {
            Visible = false;
        }
    }

    protected virtual void Execute(ButtonEventArgs obj)
    {
        if (!string.IsNullOrEmpty(Command))
            IoCManager.Resolve<IClientConsoleHost>().ExecuteCommand(Command);
    }
}
