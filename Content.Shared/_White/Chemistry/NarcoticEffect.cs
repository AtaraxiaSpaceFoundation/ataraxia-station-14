using System.Threading;
using Content.Shared._White.Mood;
using Content.Shared.Chemistry.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Drunk;
using Content.Shared.Standing;
using Content.Shared.StatusEffect;
using Robust.Shared.Random;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Shared._White.Chemistry;

public sealed class NarcoticEffect : EntitySystem
{
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;
    [Dependency] private readonly StaminaSystem _stamina = default!;
    [Dependency] private readonly StandingStateSystem _standingStateSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NarcoticEffectComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<NarcoticEffectComponent, ComponentRemove>(OnRemove);
    }

    private void OnInit(EntityUid uid, NarcoticEffectComponent component, ComponentInit args)
    {
        int index = _robustRandom.Next(0, Enum.GetNames(typeof(NarcoticEffects)).Length);

        Effects(uid, component, index);
    }

    private void OnRemove(EntityUid uid, NarcoticEffectComponent component, ComponentRemove args)
    {
        if (TryComp<MovespeedModifierMetabolismComponent>(uid, out var movespeedModifierComponent))
        {
            if (movespeedModifierComponent.ModifierTimer != TimeSpan.Zero)
                Timer.Spawn(movespeedModifierComponent.ModifierTimer, () => component.CancelTokenSource.Cancel());
                return;
        }
        component.CancelTokenSource.Cancel();
    }

    private void Effects(EntityUid uid, NarcoticEffectComponent component, int index)
    {
        if(!TryComp<StandingStateComponent>(uid, out var standingComp))
            return;

        RaiseLocalEvent(uid, new MoodEffectEvent("Stimulator"));
        CancellationToken token = component.CancelTokenSource.Token;

        TryComp<StatusEffectsComponent>(uid, out var statusEffectsComp);

        int timer = component.TimerInterval[_robustRandom.Next(0, component.TimerInterval.Count)];
        int slur = component.SlurTime[_robustRandom.Next(0, component.SlurTime.Count)];

        switch (Enum.GetValues(typeof(NarcoticEffects)).GetValue(index))
        {
            case NarcoticEffects.TremorAndShake when _statusEffectsSystem.HasStatusEffect(uid, "Drunk", statusEffectsComp):
                Timer.SpawnRepeating(timer, () => _stamina.TakeStaminaDamage(uid, 15F), token);
                _statusEffectsSystem.TryAddTime(uid, "Drunk", TimeSpan.FromSeconds(slur), statusEffectsComp);
                break;

            case NarcoticEffects.Shake when _statusEffectsSystem.HasStatusEffect(uid, "Drunk", statusEffectsComp):
                _statusEffectsSystem.TryAddTime(uid, "Drunk", TimeSpan.FromSeconds(slur), statusEffectsComp);
                break;

            case NarcoticEffects.StunAndShake when _statusEffectsSystem.HasStatusEffect(uid, "Drunk", statusEffectsComp):
                Timer.SpawnRepeating(timer, () => _standingStateSystem.TryLieDown(uid, standingComp), token);
                _statusEffectsSystem.TryAddTime(uid, "Drunk", TimeSpan.FromSeconds(slur), statusEffectsComp);
                break;

            case NarcoticEffects.Stun:
                Timer.SpawnRepeating(timer, () => _standingStateSystem.TryLieDown(uid, standingComp), token);
                break;

            case NarcoticEffects.TremorAndShake:
                Timer.SpawnRepeating(timer, () => _stamina.TakeStaminaDamage(uid, 15F), token);
                _statusEffectsSystem.TryAddStatusEffect<DrunkComponent>(uid, "Drunk", TimeSpan.FromSeconds(slur), true, statusEffectsComp);
                break;

            case NarcoticEffects.Tremor:
                Timer.SpawnRepeating(timer, () => _stamina.TakeStaminaDamage(uid, 15F), token);
                break;

            case NarcoticEffects.Shake:
                _statusEffectsSystem.TryAddStatusEffect<DrunkComponent>(uid, "Drunk", TimeSpan.FromSeconds(slur), true, statusEffectsComp);
                break;

            case NarcoticEffects.StunAndShake:
                Timer.SpawnRepeating(timer, () => _standingStateSystem.TryLieDown(uid, standingComp), token);
                _statusEffectsSystem.TryAddStatusEffect<DrunkComponent>(uid, "Drunk", TimeSpan.FromSeconds(slur), true, statusEffectsComp);
                break;
        }
    }
}
