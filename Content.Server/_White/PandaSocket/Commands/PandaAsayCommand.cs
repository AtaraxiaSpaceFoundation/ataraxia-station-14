using System.Net;
using Content.Server.Chat.Managers;
using Content.Server._White.PandaSocket.Interfaces;
using Content.Server._White.PandaSocket.Main;

namespace Content.Server._White.PandaSocket.Commands;

public sealed class PandaAsayCommand : IPandaCommand
{
    public string Name => "asay";
    public Type RequestMessageType => typeof(UtkaAsayRequest);
    public void Execute(IPandaStatusHandlerContext context, PandaBaseMessage baseMessage)
    {
        if (baseMessage is not UtkaAsayRequest message) return;
        if(string.IsNullOrWhiteSpace(message.Message) || string.IsNullOrWhiteSpace(message.ACkey)) return;

        var ckey = message.ACkey;
        var chatManager = IoCManager.Resolve<IChatManager>();

        chatManager.SendHookAdminChat(ckey, message.Message);

        Response(context);
    }

    public void Response(IPandaStatusHandlerContext context, PandaBaseMessage? message = null)
    {
        context.RespondAsync("Success", HttpStatusCode.OK);
    }
}
