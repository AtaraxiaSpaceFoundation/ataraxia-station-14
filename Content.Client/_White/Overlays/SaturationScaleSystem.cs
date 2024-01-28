using Content.Shared.GameTicking;
using Content.Shared._White.Overlays;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client._White.Overlays;

public sealed class SaturationScaleSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly ILightManager _lightManager = default!;

    private SaturationScaleOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SaturationScaleComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SaturationScaleComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<SaturationScaleComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<SaturationScaleComponent, PlayerDetachedEvent>(OnPlayerDetached);

        SubscribeNetworkEvent<RoundRestartCleanupEvent>(RoundRestartCleanup);

        _overlay = new();
    }

    private void RoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _overlayMan.RemoveOverlay(_overlay);
    }

    private void OnPlayerDetached(EntityUid uid, SaturationScaleComponent component, PlayerDetachedEvent args)
    {
        if (_player.LocalSession != args.Player)
            return;

        _overlayMan.RemoveOverlay(_overlay);
    }

    private void OnPlayerAttached(EntityUid uid, SaturationScaleComponent component, PlayerAttachedEvent args)
    {
        if (_player.LocalSession != args.Player)
            return;

        _overlayMan.AddOverlay(_overlay);
    }

    private void OnShutdown(EntityUid uid, SaturationScaleComponent component, ComponentShutdown args)
    {
        if (_player.LocalSession?.AttachedEntity != uid)
            return;

        _overlayMan.RemoveOverlay(_overlay);
    }

    private void OnInit(EntityUid uid, SaturationScaleComponent component, ComponentInit args)
    {
        if (_player.LocalSession?.AttachedEntity != uid)
            return;

        _overlayMan.AddOverlay(_overlay);
    }
}
