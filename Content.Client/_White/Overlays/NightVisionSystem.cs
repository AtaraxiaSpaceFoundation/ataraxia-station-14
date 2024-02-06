using Content.Shared._Miracle.Systems;
using Content.Shared.GameTicking;
using Content.Shared._White.Overlays;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client._White.Overlays;

public sealed class NightVisionSystem : SharedNightVisionSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly ILightManager _lightManager = default!;

    private NightVisionOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NightVisionComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<NightVisionComponent, PlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRestart);

        _overlay = new NightVisionOverlay();
    }

    private void OnPlayerAttached(EntityUid uid, NightVisionComponent component, PlayerAttachedEvent args)
    {
        UpdateNightVision(uid, component.IsActive);
    }

    private void OnPlayerDetached(EntityUid uid, NightVisionComponent component, PlayerDetachedEvent args)
    {
        UpdateNightVision(uid, false);
    }

    protected override void UpdateNightVision(EntityUid uid, bool active)
    {
        if (_player.LocalSession?.AttachedEntity != uid)
            return;

        if (active)
            _overlayMan.AddOverlay(_overlay);
        else
            _overlayMan.RemoveOverlay(_overlay);

        _lightManager.DrawLighting = !active;
    }

    private void OnRestart(RoundRestartCleanupEvent ev)
    {
        _overlayMan.RemoveOverlay(_overlay);
        _lightManager.DrawLighting = true;
    }
}
