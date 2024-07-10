using Content.Shared.Standing;
using Robust.Shared.Configuration;

namespace Content.Shared._White.Standing;

public sealed class StandingStateSystem : EntitySystem
{

    [Dependency] private readonly INetConfigurationManager _cfg = default!;
    public override void Initialize()
    {
        SubscribeNetworkEvent<CheckAutoGetUpEvent>(OnCheckAutoGetUp);
    }


    private void OnCheckAutoGetUp(CheckAutoGetUpEvent ev, EntitySessionEventArgs args)
    {
        if (!args.SenderSession.AttachedEntity.HasValue)
            return;

        var uid = args.SenderSession.AttachedEntity.Value;

        if (!TryComp(uid, out StandingStateComponent? standing))
            return;

        standing.AutoGetUp = _cfg.GetClientCVar(args.SenderSession.Channel, WhiteCVars.AutoGetUp);
        Dirty(args.SenderSession.AttachedEntity.Value, standing);
    }
}
