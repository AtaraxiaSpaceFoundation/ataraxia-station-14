using Content.Server.Chat.Managers;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Random;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class RollCommand : IConsoleCommand
    {
        public string Command => "roll";
        public string Description => "Roll a number from 1 to specified value.";
        public string Help => $"Usage: {Command} <value:int>.";


        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (shell.Player == null)
            {
                shell.WriteLine("You cannot use this command from the server console.");
                return;
            }

            if (args.Length != 1)
            {
                shell.WriteLine(Help);
                return;
            }

            if (!int.TryParse(args[0], out var maxNum))
            {
                shell.WriteLine(Help);
                return;
            }

            var random = IoCManager.Resolve<IRobustRandom>();
            var chatManager = IoCManager.Resolve<IChatManager>();
            chatManager.DispatchServerAnnouncement($"{shell.Player.Name} has thrown the D{maxNum} and the {random.Next(1, maxNum)} rolled.");

        }
    }
}
