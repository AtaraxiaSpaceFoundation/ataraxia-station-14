using Content.Server.Administration;
using Content.Server.Database;
using Content.Server._White.PandaSocket.Interfaces;
using Content.Server._White.PandaSocket.Main;

namespace Content.Server._White.PandaSocket.Commands;

public sealed class PandaUnJobBanCommand : IPandaCommand
{
    public string Name => "unjobban";
    public Type RequestMessageType => typeof(UtkaUnJobBanRequest);
    public async void Execute(IPandaStatusHandlerContext context, PandaBaseMessage baseMessage)
    {
        if (baseMessage is not UtkaUnJobBanRequest message) return;

        var dbMan = IoCManager.Resolve<IServerDbManager>();
        var locator = IoCManager.Resolve<IPlayerLocator>();
        IoCManager.InjectDependencies(this);

        var located = await locator.LookupIdByNameOrIdAsync(message.ACkey!);
        if (located == null)
        {
            UtkaSendResponse(false, context);
            return;
        }

        var player = located.UserId;

        var ban = await dbMan.GetServerRoleBanAsync(message.Bid!.Value);
        if (ban == null || ban.Unban != null)
        {
            UtkaSendResponse(false, context);
            return;
        }

        var adminData = await dbMan.GetAdminDataForAsync(player);
        if (adminData?.AdminRank == null || ban.ServerName != "unknown" && adminData.AdminServer is not (null or "unknown") && adminData.AdminServer != ban.ServerName)
        {
            UtkaSendResponse(false, context);
            return;
        }

        await dbMan.AddServerRoleUnbanAsync(new ServerRoleUnbanDef(message.Bid!.Value, player, DateTimeOffset.Now));

        UtkaSendResponse(true, context);
    }

    public void Response(IPandaStatusHandlerContext context, PandaBaseMessage? message = null)
    {
        context.RespondJsonAsync(message!);
    }

    private void UtkaSendResponse(bool unbanned, IPandaStatusHandlerContext context)
    {
        var utkaResponse = new UtkaUnJobBanResponse()
        {
            Unbanned = unbanned
        };

        Response(context, utkaResponse);
    }
}
