using Content.Shared._Miracle.Systems;
using Content.Shared.GameTicking;
using Content.Shared._White.Overlays;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client._White.Overlays;

public sealed class ThermalVisionSystem : SharedEnhancedVisionSystem<ThermalVisionComponent,
    TemporaryThermalVisionComponent, ToggleThermalVisionEvent>
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly NightVisionSystem _nv = default!;

    private ThermalVisionOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThermalVisionComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<ThermalVisionComponent, PlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRestart);

        SubscribeLocalEvent<TemporaryThermalVisionComponent, ComponentInit>(OnTempInit);
        SubscribeLocalEvent<TemporaryThermalVisionComponent, ComponentRemove>(OnTempRemove);
        SubscribeLocalEvent<TemporaryThermalVisionComponent, PlayerAttachedEvent>(OnTempPlayerAttached);
        SubscribeLocalEvent<TemporaryThermalVisionComponent, PlayerDetachedEvent>(OnTempPlayerDetached);

        _overlay = new ThermalVisionOverlay();
    }

    private void OnTempPlayerAttached(Entity<TemporaryThermalVisionComponent> ent, ref PlayerAttachedEvent args)
    {
        UpdateThermalVision(args.Player, true);
    }

    private void OnTempPlayerDetached(Entity<TemporaryThermalVisionComponent> ent, ref PlayerDetachedEvent args)
    {
        UpdateThermalVision(args.Player, false);
    }

    private void OnTempRemove(Entity<TemporaryThermalVisionComponent> ent, ref ComponentRemove args)
    {
        if (TryComp(ent, out ThermalVisionComponent? thermalVision) && thermalVision.IsActive)
            return;

        UpdateEnhancedVision(ent, false);
    }

    private void OnTempInit(Entity<TemporaryThermalVisionComponent> ent, ref ComponentInit args)
    {
        UpdateEnhancedVision(ent, true);
    }

    private void OnPlayerAttached(EntityUid uid, ThermalVisionComponent component, PlayerAttachedEvent args)
    {
        if (!component.IsActive && HasComp<TemporaryThermalVisionComponent>(args.Entity))
            return;

        UpdateThermalVision(args.Player, component.IsActive);
    }

    private void OnPlayerDetached(EntityUid uid, ThermalVisionComponent component, PlayerDetachedEvent args)
    {
        UpdateThermalVision(args.Player, false);
    }

    private void UpdateThermalVision(ICommonSession player, bool active)
    {
        if (_player.LocalSession != player)
            return;

        _nv.UpdateOverlay(active);
        UpdateThermalVision(active);
    }

    protected override void UpdateEnhancedVision(EntityUid uid, bool active)
    {
        if (_player.LocalSession?.AttachedEntity != uid)
            return;

        _nv.UpdateOverlay(active);
        UpdateThermalVision(active);
    }

    public void UpdateThermalVision(bool active)
    {
        if (_player.LocalEntity == null)
        {
            _overlay.Reset();
            _overlayMan.RemoveOverlay(_overlay);
            return;
        }

        var uid = _player.LocalEntity.Value;
        active |= TryComp<ThermalVisionComponent>(uid, out var thermal) && thermal.IsActive ||
                  HasComp<TemporaryThermalVisionComponent>(uid);
        if (active)
            _overlayMan.AddOverlay(_overlay);
        else
        {
            _overlay.Reset();
            _overlayMan.RemoveOverlay(_overlay);
        }
    }

    private void OnRestart(RoundRestartCleanupEvent ev)
    {
        _overlay.Reset();
        _overlayMan.RemoveOverlay(_overlay);
    }
}
