using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Shared.Administration;
using Content.Shared.Changeling;
using Robust.Shared.Console;
using Robust.Shared.Enums;

namespace Content.Server.Chat.Commands;

[AnyCommand]
internal sealed class ChangelingChatCommand : IConsoleCommand
{
    public string Command => "gsay";
    public string Description => "Send changeling Hive message";
    public string Help => "gsay <text>";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteError("This command cannot be run from the server.");
            return;
        }

        if (player.AttachedEntity is not { Valid: true } entity)
            return;

        if (args.Length < 1)
            return;

        var entityManager = IoCManager.Resolve<IEntityManager>();

        if (!entityManager.HasComponent<ChangelingComponent>(entity))
            return;


        var message = string.Join(" ", args).Trim();
        if (string.IsNullOrEmpty(message))
            return;

        entityManager.System<ChatSystem>().TrySendInGameOOCMessage(entity, message,
            InGameOOCChatType.Changeling, false, shell, player);
    }
}
