using Content.Server.GameTicking.Rules.Components;
using Content.Server._White.AspectsSystem.Aspects.Components;
using Content.Server._White.AspectsSystem.Base;
using Content.Server._White.Other;

namespace Content.Server._White.AspectsSystem.Aspects;

public sealed class DarknessAspect : AspectSystem<DarknessAspectComponent>
{
    protected override void Started(EntityUid uid, DarknessAspectComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        var query = EntityQueryEnumerator<LightMarkComponent>();
        while (query.MoveNext(out var ent, out _))
        {
            EntityManager.DeleteEntity(ent);
        }
    }
}
