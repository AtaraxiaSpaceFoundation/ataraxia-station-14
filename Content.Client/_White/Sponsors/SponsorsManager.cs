using System.Diagnostics.CodeAnalysis;
using Content.Shared._White.Sponsors;
using Robust.Shared.Network;

namespace Content.Client._White.Sponsors;

public sealed class SponsorsManager
{
    [Dependency] private readonly IClientNetManager _netMgr = default!;

    private SponsorInfo? _info;

    public void Initialize()
    {
        _netMgr.RegisterNetMessage<MsgSponsorInfo>(msg => _info = msg.Info);
    }

    public bool TryGetInfo([NotNullWhen(true)] out SponsorInfo? sponsor)
    {
        sponsor = _info;
        return _info != null;
    }
}
