using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using Content.Server._White.PandaSocket.Interfaces;

namespace Content.Server._White.PandaSocket.Main;

public sealed partial class PandaStatusHost
{

    private void RegisterHandlers()
    {
        AddHandler(HandleChecker);
        AddHandler(HandleQueryCheck);
        AddHandler(HandleRequest);
    }

    private async Task<bool> HandleChecker(IPandaStatusHandlerContext context)
    {
        if (!context.IsGetLike || context.Url!.AbsolutePath != "/checker")
        {
            return false;
        }

        await context.RespondAsync("Checking panda socket, ukta loh.", (HttpStatusCode) 418);
        return true;
    }

    private async Task<bool> HandleQueryCheck(IPandaStatusHandlerContext context)
    {
        if (!context.IsGetLike || context.Url!.AbsolutePath != "/querycheck")
        {
            return false;
        }

        var query = HttpUtility.ParseQueryString(context.Url.Query);
        var text = query["text"] ?? "None";

        await context.RespondAsync(text, (HttpStatusCode) 418);

        return true;
    }

    private async Task<bool> HandleRequest(IPandaStatusHandlerContext context)
    {
        if (!context.IsPostLike || context.Url!.AbsolutePath != "/request")
        {
            return false;
        }

        if (!ValidatePostMessage(context.RequestBody, out var message) || message == null)
            return false;

        ExecuteCommand(context, message);
        return true;
    }
}
