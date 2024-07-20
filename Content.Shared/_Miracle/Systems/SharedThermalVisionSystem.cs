using Content.Shared._White.Overlays;
using Content.Shared.Actions;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._Miracle.Systems;

public abstract class SharedThermalVisionSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThermalVisionComponent, ToggleThermalVisionEvent>(OnToggle);
        SubscribeLocalEvent<ThermalVisionComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ThermalVisionComponent, ComponentRemove>(OnRemove);
    }

    private void OnRemove(EntityUid uid, ThermalVisionComponent component, ComponentRemove args)
    {
        _actions.RemoveAction(uid, component.ToggleActionEntity);

        if (HasComp<TemporaryThermalVisionComponent>(uid))
            return;

        UpdateThermalVision(uid, false);
    }

    private void OnInit(EntityUid uid, ThermalVisionComponent component, ComponentInit args)
    {
        _actions.AddAction(uid, ref component.ToggleActionEntity, component.ToggleAction);

        if (!component.IsActive && HasComp<TemporaryThermalVisionComponent>(uid))
            return;

        UpdateThermalVision(uid, component.IsActive);
    }

    protected virtual void UpdateThermalVision(EntityUid uid, bool active) { }

    private void OnToggle(EntityUid uid, ThermalVisionComponent component, ToggleThermalVisionEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        component.IsActive = !component.IsActive;

        if (component.IsActive && component.ActivateSound != null)
            _audio.PlayPredicted(component.ActivateSound, uid, uid);
        else if (component.DeactivateSound != null)
            _audio.PlayPredicted(component.DeactivateSound, uid, uid);

        args.Handled = true;

        if (!component.IsActive && HasComp<TemporaryThermalVisionComponent>(uid))
            return;

        UpdateThermalVision(uid, component.IsActive);
    }
}
