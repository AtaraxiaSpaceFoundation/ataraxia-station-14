using System.Text;
using Content.Server.Speech;
using Content.Shared.StatusEffect;
using Robust.Shared.Random;

namespace Content.Server._White.Accent.Bloodloss;

public sealed class BloodLossAccent : EntitySystem
{
    [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BloodLossAccentComponent, AccentGetEvent>(OnAccent);
    }

    public void StartBloodLossAccent(EntityUid uid, TimeSpan time, bool refresh, StatusEffectsComponent? status = null)
    {
        if (!Resolve(uid, ref status, false))
            return;

        _statusEffectsSystem.TryAddStatusEffect<BloodLossAccentComponent>(uid, "BloodLoss", time, refresh, status);
    }

    public void StopBloodLossAccent(EntityUid uid, double timeRemoved)
    {
        _statusEffectsSystem.TryRemoveTime(uid, "BloodLoss", TimeSpan.FromSeconds(timeRemoved));
    }

    private void OnAccent(EntityUid uid, BloodLossAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message, component);
    }

    public string Accentuate(string message, BloodLossAccentComponent component)
    {
        if (string.IsNullOrEmpty(message))
        {
            return message;
        }

        var result = new StringBuilder();
        string[] words = message.Split(' ');

        foreach (var word in words)
        {
            if (word.Length >= 3 && _random.NextDouble() < component.ReplaceProb)
            {
                int start = Random.Shared.Next(1, word.Length - 1);
                int end = start + Random.Shared.Next(1, word.Length - start);

                result.Append(word.Substring(0, start) + component.ToReplace + word.Substring(end));
            }
            else
            {
                result.Append(word);
            }

            result.Append(' ');
        }

        return result.ToString().TrimEnd();
    }
}
