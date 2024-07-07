using Content.Server.Chat.Systems;
using Content.Shared.Damage;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Ataraxia.Gasp
{
    public sealed class GaspSystem : EntitySystem
    {
        [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly ChatSystem _chatSystem = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<GaspComponent, DamageChangedEvent>(OnDamaged);
        }

        private void OnDamaged(EntityUid uid, GaspComponent component, DamageChangedEvent args)
        {
            if (!_mobStateSystem.IsAlive(uid))
                return;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            var curTime = _timing.CurTime;

            var query = EntityQueryEnumerator<GaspComponent, MobStateComponent>();
            while (query.MoveNext(out var uid, out var comp, out var state))
            {
                if (state.CurrentState != MobState.Critical)
                    continue;

                if (curTime < comp.NextGaspTime)
                    return;

                comp.NextGaspTime = curTime + TimeSpan.FromSeconds(comp.GaspInterval);
                _chatSystem.TryEmoteWithChat(uid, "Gasp", ignoreActionBlocker: true);
            }
        }
    }
}
