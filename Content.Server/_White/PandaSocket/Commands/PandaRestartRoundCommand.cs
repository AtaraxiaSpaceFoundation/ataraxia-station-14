using Content.Server.GameTicking;
using Content.Server._White.PandaSocket.Interfaces;
using Content.Server._White.PandaSocket.Main;

namespace Content.Server._White.PandaSocket.Commands;

public sealed class PandaRestartRoundCommand : IPandaCommand
{
    public string Name => "restartround";
    public Type RequestMessageType => typeof(UtkaRestartRoundRequest);
    public void Execute(IPandaStatusHandlerContext context, PandaBaseMessage baseMessage)
    {
        if (baseMessage is not UtkaRestartRoundRequest message) return;

        IoCManager.InjectDependencies(this);

        EntitySystem.Get<GameTicker>().RestartRound();

        var response = new UtkaRestartRoundResponse()
        {
            Restarted = true
        };

        Response(context, response);
    }

    public void Response(IPandaStatusHandlerContext context, PandaBaseMessage? message = null)
    {
        context.RespondJsonAsync(message!);
    }
}
