using Content.Shared._White.Overlays;
using Content.Shared.Actions;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._Miracle.Systems;

public abstract class SharedNightVisionSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NightVisionComponent, ToggleNightVisionEvent>(OnToggle);
        SubscribeLocalEvent<NightVisionComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<NightVisionComponent, ComponentRemove>(OnRemove);
    }

    private void OnRemove(EntityUid uid, NightVisionComponent component, ComponentRemove args)
    {
        _actions.RemoveAction(uid, component.ToggleActionEntity);
        UpdateNightVision(uid, false);
    }

    private void OnInit(EntityUid uid, NightVisionComponent component, ComponentInit args)
    {
        _actions.AddAction(uid, ref component.ToggleActionEntity, component.ToggleAction);
        UpdateNightVision(uid, component.IsActive);
    }

    protected virtual void UpdateNightVision(EntityUid uid, bool active) { }

    private void OnToggle(EntityUid uid, NightVisionComponent component, ToggleNightVisionEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        component.IsActive = !component.IsActive;
        _audio.PlayPredicted(component.ToggleSound, uid, uid);
        UpdateNightVision(uid, component.IsActive);

        args.Handled = true;
    }
}
