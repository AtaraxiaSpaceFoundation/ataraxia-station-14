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

        SubscribeLocalEvent<TemporaryNightVisionComponent, ComponentInit>(OnTempInit);
        SubscribeLocalEvent<TemporaryNightVisionComponent, ComponentRemove>(OnTempRemove);
        SubscribeLocalEvent<TemporaryNightVisionComponent, PlayerAttachedEvent>(OnTempPlayerAttached);
        SubscribeLocalEvent<TemporaryNightVisionComponent, PlayerDetachedEvent>(OnTempPlayerDetached);

        _overlay = new NightVisionOverlay();
    }

    private void OnTempPlayerAttached(Entity<TemporaryNightVisionComponent> ent, ref PlayerAttachedEvent args)
    {
        UpdateNightVision(args.Player, true);
    }

    private void OnTempPlayerDetached(Entity<TemporaryNightVisionComponent> ent, ref PlayerDetachedEvent args)
    {
        UpdateNightVision(args.Player, false);
    }

    private void OnTempRemove(Entity<TemporaryNightVisionComponent> ent, ref ComponentRemove args)
    {
        if (TryComp(ent, out NightVisionComponent? nightVision) && nightVision.IsActive)
            return;

        UpdateNightVision(ent, false);
    }

    private void OnTempInit(Entity<TemporaryNightVisionComponent> ent, ref ComponentInit args)
    {
        UpdateNightVision(ent, true);
    }

    private void OnPlayerAttached(EntityUid uid, NightVisionComponent component, PlayerAttachedEvent args)
    {
        if (!component.IsActive && HasComp<TemporaryNightVisionComponent>(args.Entity))
            return;

        UpdateNightVision(args.Player, component.IsActive);
    }

    private void OnPlayerDetached(EntityUid uid, NightVisionComponent component, PlayerDetachedEvent args)
    {
        UpdateNightVision(args.Player, false);
    }

    private void UpdateNightVision(ICommonSession player, bool active)
    {
        if (_player.LocalSession != player)
            return;

        UpdateOverlay(active);
        UpdateNightVision(active);
    }

    protected override void UpdateNightVision(EntityUid uid, bool active)
    {
        if (_player.LocalSession?.AttachedEntity != uid)
            return;

        UpdateOverlay(active);
        UpdateNightVision(active);
    }

    public void UpdateOverlay(bool active)
    {
        if (_player.LocalEntity == null)
        {
            _overlayMan.RemoveOverlay(_overlay);
            return;
        }

        var uid = _player.LocalEntity.Value;
        active |= TryComp<NightVisionComponent>(uid, out var nv) && nv.IsActive ||
                  TryComp<ThermalVisionComponent>(uid, out var thermal) && thermal.IsActive ||
                  HasComp<TemporaryNightVisionComponent>(uid) ||
                  HasComp<TemporaryThermalVisionComponent>(uid);
        if (active)
            _overlayMan.AddOverlay(_overlay);
        else
            _overlayMan.RemoveOverlay(_overlay);
    }

    private void UpdateNightVision(bool active)
    {
        _lightManager.DrawLighting = !active;
    }

    private void OnRestart(RoundRestartCleanupEvent ev)
    {
        _overlayMan.RemoveOverlay(_overlay);
        _lightManager.DrawLighting = true;
    }
}
