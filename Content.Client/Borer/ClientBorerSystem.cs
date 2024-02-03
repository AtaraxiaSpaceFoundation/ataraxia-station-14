using Content.Shared.Borer;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;

namespace Content.Client.Borer;

/// <summary>
/// This handles...
/// </summary>
public sealed class ClientBorerSystem : EntitySystem
{
    [Dependency] private readonly IResourceCache _client = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IPlayerManager _playerMgr = default!;

    public override void Initialize()
    {
        SubscribeNetworkEvent<BorerOverlayResponceEvent>(OnOverlayResponce);
    }

    private void OnOverlayResponce(BorerOverlayResponceEvent ev)
    {
        if(!_overlayManager.HasOverlay<BorerOverlay>())
        _overlayManager.AddOverlay(new BorerOverlay(
            _entManager,
            _playerMgr,
            _client));
    }
}
