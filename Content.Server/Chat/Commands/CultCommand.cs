using Content.Server.Chat.Systems;
using Content.Shared.Administration;
using Content.Shared._White.Cult;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using CultistComponent = Content.Shared._White.Cult.Components.CultistComponent;

namespace Content.Server.Chat.Commands
{
    [AnyCommand]
    internal sealed class CultCommand : IConsoleCommand
    {
        public string Command => "csay";

        public string Description => "Send cult message";

        public string Help => "csay <text>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (shell.Player is not { } player)
            {
                shell.WriteError("This command cannot be run from the server.");
                return;
            }

            if (player.AttachedEntity is not { Valid: true } entity)
                return;

            if (player.Status != SessionStatus.InGame)
                return;

            if (args.Length < 1)
                return;

            var entityManager = IoCManager.Resolve<EntityManager>();

            if (!entityManager.HasComponent<CultistComponent>(entity))
            {
                return;
            }

            var message = string.Join(" ", args).Trim();
            if (string.IsNullOrEmpty(message))
                return;

            entityManager.System<ChatSystem>()
                .TrySendInGameOOCMessage(entity, message, InGameOOCChatType.Cult, false, shell, player);
        }
    }
}
