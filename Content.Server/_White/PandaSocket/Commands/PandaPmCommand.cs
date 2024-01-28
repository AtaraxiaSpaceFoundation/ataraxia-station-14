using System.Net;
using Content.Server.Administration.Systems;
using Content.Server._White.PandaSocket.Interfaces;
using Content.Server._White.PandaSocket.Main;
using Robust.Server.Player;

namespace Content.Server._White.PandaSocket.Commands;

public sealed class PandaPmCommand : IPandaCommand
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public string Name => "discordpm";
    public Type RequestMessageType => typeof(UtkaPmRequest);
    public void Execute(IPandaStatusHandlerContext context, PandaBaseMessage baseMessage)
    {
        if (baseMessage is not UtkaPmRequest message) return;
        var _bwoink = EntitySystem.Get<BwoinkSystem>();
        IoCManager.InjectDependencies(this);

        var toUtkaMessage = new UtkaPmResponse();

        if(string.IsNullOrWhiteSpace(message.Message) || string.IsNullOrWhiteSpace(message.Sender) || string.IsNullOrWhiteSpace(message.Receiver))
        {
            toUtkaMessage.Message = false;
            Response(context, toUtkaMessage);
            return;
        }

        if (!_playerManager.TryGetUserId(message.Receiver, out var reciever))
        {
            toUtkaMessage.Message = false;
            Response(context, toUtkaMessage);
            return;
        }

        _bwoink.SendUtkaBwoinkMessage(reciever, message.Sender, message.Message);

        toUtkaMessage.Message = true;

        Response(context, toUtkaMessage);
    }

    public void Response(IPandaStatusHandlerContext context, PandaBaseMessage? message = null)
    {
        context.RespondJsonAsync(message!);
    }
}
