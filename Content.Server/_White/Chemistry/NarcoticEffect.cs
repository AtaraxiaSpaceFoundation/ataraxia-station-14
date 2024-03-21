using System.Threading;
using Content.Shared._White.Mood;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Drunk;
using Content.Shared.StatusEffect;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server._White.Chemistry;

/// <summary>
/// This handles system?
/// </summary>
public sealed class NarcoticEffect : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private static readonly IPrototypeManager _prototypeManager = default!;
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
        int index = _robustRandom.Next(0, component.Effects.Count);

        Effects(uid, component, index);
    }

    private void OnRemove(EntityUid uid, NarcoticEffectComponent component, ComponentRemove args)
    {
        component.cancelTokenSource.Cancel();
    }

    private void Effects(EntityUid uid, NarcoticEffectComponent component, int index)
    {
        var damageSpecifier = new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>("Poison"), 5);
        RaiseLocalEvent(uid, new MoodEffectEvent("Stimulator"));
        CancellationToken token = component.cancelTokenSource.Token;

        TryComp<StatusEffectsComponent>(uid, out var statusEffectsComp);

        int timer = component.TimerInterval[_robustRandom.Next(0, component.TimerInterval.Count)];
        int slur = component.SlurTime[_robustRandom.Next(0, component.SlurTime.Count)];

        switch (component.Effects[index])
        {
            case "TremorAndShake" when _statusEffectsSystem.HasStatusEffect(uid, "Drunk", statusEffectsComp):
                Timer.SpawnRepeating(timer, () => _stamina.TakeStaminaDamage(uid, 15F), token);
                _statusEffectsSystem.TryAddTime(uid, "Drunk", TimeSpan.FromSeconds(slur), statusEffectsComp);
                break;

            case "Shake" when _statusEffectsSystem.HasStatusEffect(uid, "Drunk", statusEffectsComp):
                _statusEffectsSystem.TryAddTime(uid, "Drunk", TimeSpan.FromSeconds(slur), statusEffectsComp);
                break;

            case "DamageAndShake" when _statusEffectsSystem.HasStatusEffect(uid, "Drunk", statusEffectsComp):
                _damageableSystem.TryChangeDamage(uid, damageSpecifier, true, false);
                _statusEffectsSystem.TryAddTime(uid, "Drunk", TimeSpan.FromSeconds(slur), statusEffectsComp);
                break;

            case "Damage":
                _damageableSystem.TryChangeDamage(uid, damageSpecifier, true, false);
                break;

            case "TremorAndShake":
                Timer.SpawnRepeating(timer, () => _stamina.TakeStaminaDamage(uid, 15F), token);
                _statusEffectsSystem.TryAddStatusEffect<DrunkComponent>(uid, "Drunk", TimeSpan.FromSeconds(slur), true, statusEffectsComp);
                break;

            case "Tremor":
                Timer.SpawnRepeating(timer, () => _stamina.TakeStaminaDamage(uid, 15F), token);
                break;

            case "Shake":
                _statusEffectsSystem.TryAddStatusEffect<DrunkComponent>(uid, "Drunk", TimeSpan.FromSeconds(slur), true, statusEffectsComp);
                break;

            case "DamageAndShake":
                _damageableSystem.TryChangeDamage(uid, damageSpecifier, true, false);
                _statusEffectsSystem.TryAddStatusEffect<DrunkComponent>(uid, "Drunk", TimeSpan.FromSeconds(slur), true, statusEffectsComp);
                break;
        }
    }
}
