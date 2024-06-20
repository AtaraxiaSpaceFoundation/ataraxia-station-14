using Content.Client.GameTicking.Managers;
using Content.Shared.Administration;
using Content.Shared.GameTicking;
using Robust.Shared.Console;
using Robust.Shared.Network;

namespace Content.Client.GameTicking.Commands
{
    [AnyCommand]
    public sealed class ShowManifestCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntitySystemManager _entitySystem = default!;

        public string Command => "showmanifest";
        public string Description => "Shows round end summary window";
        public string Help => "Usage: showmanifest";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var ticker = _entitySystem.GetEntitySystem<ClientGameTicker>();
            var window = ticker._window;

            if (!ticker.IsGameStarted && window != null)
            {
                window.OpenCentered();
                return;
            }
            shell.WriteLine("You can't open manifest right now");
        }
    }
}
