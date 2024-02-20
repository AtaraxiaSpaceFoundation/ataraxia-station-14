using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server._White.Commands;

[UsedImplicitly]
[AdminCommand(AdminFlags.Permissions)]
public sealed class ForceDeadminCommand : IConsoleCommand
{
    public string Command => "forcedeadmin";
    public string Description => "Forces someone to deadmin.";
    public string Help => "forcedeadmin <Ckey>";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1)
        {
            shell.WriteLine("Didn't specify player.");
            return;
        }

        var ckey = args[0];
        var playerManager = IoCManager.Resolve<IPlayerManager>();

        if (!playerManager.TryGetSessionByUsername(ckey, out var player))
        {
            shell.WriteLine($"Couldn't find player {ckey}");
            return;
        }

        var mgr = IoCManager.Resolve<IAdminManager>();
        mgr.DeAdmin(player);

        shell.WriteLine($"Deadmined {ckey}");
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length == 1 ? CompletionResult.FromHintOptions(CompletionHelper.SessionNames(), "<Player ckey>") : CompletionResult.Empty;
    }
}
