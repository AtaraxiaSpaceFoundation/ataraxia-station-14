using Content.Shared._White.Overlays;
using Content.Shared.Actions;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._Miracle.Systems;

public abstract class SharedEnhancedVisionSystem<TComp, TTempComp, TEvent> : EntitySystem
    where TComp : BaseEnhancedVisionComponent
    where TEvent : InstantActionEvent
    where TTempComp : Component
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TComp, TEvent>(OnToggle);
        SubscribeLocalEvent<TComp, ComponentInit>(OnInit);
        SubscribeLocalEvent<TComp, ComponentRemove>(OnRemove);
    }

    private void OnRemove(EntityUid uid, TComp component, ComponentRemove args)
    {
        _actions.RemoveAction(uid, component.ToggleActionEntity);

        if (HasComp<TTempComp>(uid))
            return;

        UpdateEnhancedVision(uid, false);
    }

    private void OnInit(EntityUid uid, TComp component, ComponentInit args)
    {
        _actions.AddAction(uid, ref component.ToggleActionEntity, component.ToggleAction);

        if (!component.IsActive && HasComp<TTempComp>(uid))
            return;

        UpdateEnhancedVision(uid, component.IsActive);
    }

    protected virtual void UpdateEnhancedVision(EntityUid uid, bool active) { }

    private void OnToggle(EntityUid uid, TComp component, TEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        component.IsActive = !component.IsActive;

        _audio.PlayPredicted(component.IsActive ? component.ActivateSound : component.DeactivateSound, uid, uid);

        args.Handled = true;

        if (!component.IsActive && HasComp<TTempComp>(uid))
            return;

        UpdateEnhancedVision(uid, component.IsActive);
    }
}
