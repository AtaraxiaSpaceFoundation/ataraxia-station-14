using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules.Components;
using Content.Server._White.AspectsSystem.Aspects.Components;
using Content.Server._White.AspectsSystem.Base;
using Content.Server.Holiday.Christmas;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;

namespace Content.Server._White.AspectsSystem.Aspects;

public sealed class RandomItemAspect : AspectSystem<RandomItemAspectComponent>
{
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly RandomGiftSystem _giftSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(HandleLateJoin);
    }

    protected override void Started(EntityUid uid, RandomItemAspectComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        var query = EntityQueryEnumerator<HumanoidAppearanceComponent>();
        while (query.MoveNext(out var ent, out _))
        {
            GiveItem(ent, component);
        }

    }

    private void HandleLateJoin(PlayerSpawnCompleteEvent ev)
    {
        var query = EntityQueryEnumerator<RandomItemAspectComponent, GameRuleComponent>();
        while (query.MoveNext(out var ruleEntity, out var component, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(ruleEntity, gameRule))
                continue;

            if (!ev.LateJoin)
                return;

            var mob = ev.Mob;

            GiveItem(mob, component);
        }
    }

    #region Helpers

    private void GiveItem(EntityUid player, RandomItemAspectComponent component)
    {
        component.Item ??= _giftSystem.PickRandomItem();

        if (component.Item == null)
            return;

        var transform = CompOrNull<TransformComponent>(player);

        if(transform == null)
            return;

        if(!HasComp<HandsComponent>(player))
            return;

        var weaponEntity = EntityManager.SpawnEntity(component.Item, transform.Coordinates);

        _handsSystem.PickupOrDrop(player, weaponEntity);
    }

    #endregion
}
