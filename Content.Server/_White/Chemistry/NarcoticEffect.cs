using System.Threading;
using Content.Server.Speech.EntitySystems;
using Content.Server.Stunnable;
using Content.Shared._White.Mood;
using Content.Shared.Damage.Systems;
using Content.Shared.Drunk;
using Content.Shared.StatusEffect;
using Robust.Shared.Random;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server._White.Chemistry;

/// <summary>
/// This handles system?
/// </summary>
public sealed class NarcoticEffect : EntitySystem
{
    [Dependency] private readonly StunSystem _stun = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;
    [Dependency] private readonly StaminaSystem _stamina = default!;

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
        component.CancelTokenSource.Cancel();
    }

    private void Effects(EntityUid uid, NarcoticEffectComponent component, int index)
    {
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
                Timer.SpawnRepeating(timer, () => _stun.TryParalyze(uid, TimeSpan.FromSeconds(component.StunTime), true), token);
                _statusEffectsSystem.TryAddTime(uid, "Drunk", TimeSpan.FromSeconds(slur), statusEffectsComp);
                break;

            case NarcoticEffects.Stun:
                Timer.SpawnRepeating(timer, () => _stun.TryParalyze(uid, TimeSpan.FromSeconds(component.StunTime), true), token);
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
                Timer.SpawnRepeating(timer, () => _stun.TryParalyze(uid, TimeSpan.FromSeconds(component.StunTime), true), token);
                _statusEffectsSystem.TryAddStatusEffect<DrunkComponent>(uid, "Drunk", TimeSpan.FromSeconds(slur), true, statusEffectsComp);
                break;
        }
    }
}
