using Content.Client.Administration.Managers;
using Content.Client.Ghost;
using Content.Shared.Administration;
using Content.Shared.Changeling;
using Content.Shared.Chat;
using Robust.Client.Console;
using Robust.Client.Player;
using Robust.Shared.Utility;
using CultistComponent = Content.Shared._White.Cult.Components.CultistComponent;

namespace Content.Client.Chat.Managers
{
    internal sealed class ChatManager : IChatManager
    {
        [Dependency] private readonly IClientConsoleHost _consoleHost = default!;
        [Dependency] private readonly IClientAdminManager _adminMgr = default!;
        [Dependency] private readonly IEntitySystemManager _systems = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IPlayerManager _player = default!;


        private ISawmill _sawmill = default!;

        public void Initialize()
        {
            _sawmill = Logger.GetSawmill("chat");
            _sawmill.Level = LogLevel.Info;
        }

        public void SendMessage(string text, ChatSelectChannel channel)
        {
            switch (channel)
            {
                case ChatSelectChannel.Console:
                    // run locally
                    _consoleHost.ExecuteCommand(text);
                    break;

                case ChatSelectChannel.LOOC:
                    _consoleHost.ExecuteCommand($"looc \"{CommandParsing.Escape(text)}\"");
                    break;

                case ChatSelectChannel.OOC:
                    _consoleHost.ExecuteCommand($"ooc \"{CommandParsing.Escape(text)}\"");
                    break;

                case ChatSelectChannel.Admin:
                    _consoleHost.ExecuteCommand($"asay \"{CommandParsing.Escape(text)}\"");
                    break;

                case ChatSelectChannel.Emotes:
                    _consoleHost.ExecuteCommand($"me \"{CommandParsing.Escape(text)}\"");
                    break;

                case ChatSelectChannel.Cult:
                    var localEnt = _player.LocalPlayer != null ? _player.LocalPlayer.ControlledEntity : null;
                    if (_entityManager.TryGetComponent(localEnt, out CultistComponent? comp))
                        _consoleHost.ExecuteCommand($"csay \"{CommandParsing.Escape(text)}\"");
                    break;

                case ChatSelectChannel.Dead:
                    if (_systems.GetEntitySystemOrNull<GhostSystem>() is {IsGhost: true})
                        goto case ChatSelectChannel.Local;

                    if (_adminMgr.HasFlag(AdminFlags.Admin))
                        _consoleHost.ExecuteCommand($"dsay \"{CommandParsing.Escape(text)}\"");
                    else
                        _sawmill.Warning("Tried to speak on deadchat without being ghost or admin.");
                    break;

                // TODO sepearate radio and say into separate commands.
                case ChatSelectChannel.Radio:
                case ChatSelectChannel.Local:
                    _consoleHost.ExecuteCommand($"say \"{CommandParsing.Escape(text)}\"");
                    break;

                case ChatSelectChannel.Whisper:
                    _consoleHost.ExecuteCommand($"whisper \"{CommandParsing.Escape(text)}\"");
                    break;

                case ChatSelectChannel.Changeling:
                    var localEntity = _player.LocalPlayer != null ? _player.LocalPlayer.ControlledEntity : null;
                    if (_entityManager.HasComponent<ChangelingComponent>(localEntity))
                        _consoleHost.ExecuteCommand($"gsay \"{CommandParsing.Escape(text)}\"");
                    break;


                default:
                    throw new ArgumentOutOfRangeException(nameof(channel), channel, null);
            }
        }
    }
}
