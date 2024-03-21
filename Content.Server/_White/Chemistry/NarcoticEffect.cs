using System.Threading;
using Content.Server.Stunnable;
using Content.Shared._White.Mood;
using Content.Shared.Alert;
using Content.Shared.Damage.Systems;
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
    [Dependency] private readonly StaminaSystem _stamina = default!;
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;

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
        _alertsSystem.ClearAlert(uid, AlertType.Bleeding);
    }

    private void Effects(EntityUid uid, NarcoticEffectComponent component, int index)
    {
        RaiseLocalEvent(uid, new MoodEffectEvent("Stimulator"));
        CancellationToken token = component.cancelTokenSource.Token;
        switch (component.Effects[index])
        {
            case "Stun":
                Timer.SpawnRepeating(10000, () => _stun.TryParalyze(uid, TimeSpan.FromSeconds(component.StunTime), true), token);
                break;

            case "TremorAndShake":
                Timer.SpawnRepeating(6000, () => _stamina.TakeStaminaDamage(uid, 20F), token);
                _alertsSystem.ShowAlert(uid, AlertType.Bleeding);
                break;

            case "Tremor":
                Timer.SpawnRepeating(6000, () => _stamina.TakeStaminaDamage(uid, 20F), token);
                break;

            case "Shake":
                _alertsSystem.ShowAlert(uid, AlertType.Bleeding);
                break;

            case "StunAndShake":
                Timer.SpawnRepeating(10000, () => _stun.TryParalyze(uid, TimeSpan.FromSeconds(component.StunTime), true), token);
                _alertsSystem.ShowAlert(uid, AlertType.Bleeding);
                break;
        }
    }
}
