using Content.Server.GameTicking.Rules.Components;
using Content.Server._White.AspectsSystem.Aspects.Components;
using Content.Server._White.AspectsSystem.Base;
using Content.Server._White.Other;

namespace Content.Server._White.AspectsSystem.Aspects;

public sealed class NoEngineAspect : AspectSystem<NoEngineAspectComponent>
{
    protected override void Started(EntityUid uid, NoEngineAspectComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        var query = EntityQueryEnumerator<EngineMarkComponent>();
        while (query.MoveNext(out var ent, out _))
        {
            EntityManager.DeleteEntity(ent);
        }
    }

}
