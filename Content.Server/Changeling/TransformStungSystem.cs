using Content.Server.Forensics;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;

namespace Content.Server.Changeling;

public sealed class TransformStungSystem : EntitySystem
{
    [Dependency] private readonly ChangelingSystem _ling = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TransformStungComponent, MobStateComponent, DnaComponent>();
        while (query.MoveNext(out var uid, out var stung, out var state, out var dna))
        {
            if (dna.DNA == stung.OriginalHumanoidData.Dna)
            {
                RemCompDeferred<TransformStungComponent>(uid);
                continue;
            }

            if (state.CurrentState != MobState.Alive)
                continue;

            stung.Accumulator += frameTime;

            if (stung.Accumulator < stung.Duration.TotalSeconds)
                continue;

            stung.Accumulator = 0f;

            _ling.TransformPerson(uid, stung.OriginalHumanoidData);

            RemCompDeferred<TransformStungComponent>(uid);
        }
    }
}
