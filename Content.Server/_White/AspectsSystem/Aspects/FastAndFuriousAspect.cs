using Content.Server.Cloning;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules.Components;
using Content.Server._White.AspectsSystem.Aspects.Components;
using Content.Server._White.AspectsSystem.Base;
using Content.Server._White.Other.FastAndFuriousSystem;
using Content.Shared.Cloning;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;

namespace Content.Server._White.AspectsSystem.Aspects;

public sealed class FastAndFuriousAspect : AspectSystem<FastAndFuriousAspectComponent>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(HandleLateJoin);
        SubscribeLocalEvent<MovementSpeedModifierComponent, CloningEvent>(HandleCloning);
    }

    protected override void Started(EntityUid uid, FastAndFuriousAspectComponent component, GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);
        var query = EntityQueryEnumerator<MovementSpeedModifierComponent>();
        while (query.MoveNext(out var ent, out _))
        {
            EnsureComp<FastAndFuriousComponent>(ent);
        }
    }

    protected override void Ended(EntityUid uid, FastAndFuriousAspectComponent component, GameRuleComponent gameRule,
        GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);
        var query = EntityQueryEnumerator<MovementSpeedModifierComponent>();
        while (query.MoveNext(out var ent, out _))
        {
            EnsureComp<FastAndFuriousComponent>(ent);
        }
    }

    private void HandleCloning(EntityUid uid, MovementSpeedModifierComponent component, ref CloningEvent ev)
    {
        ModifySpeedIfActive(ev.Target);
    }

    private void HandleLateJoin(PlayerSpawnCompleteEvent ev)
    {
        if (!ev.LateJoin)
            return;

        ModifySpeedIfActive(ev.Mob);
    }

    private void ModifySpeedIfActive(EntityUid mob)
    {
        var query = EntityQueryEnumerator<FastAndFuriousAspectComponent, GameRuleComponent>();
        while (query.MoveNext(out var ruleEntity, out _, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(ruleEntity, gameRule))
                continue;

            if (!HasComp<MovementSpeedModifierComponent>(mob))
                return;

            EnsureComp<FastAndFuriousComponent>(mob);
        }
    }
}
