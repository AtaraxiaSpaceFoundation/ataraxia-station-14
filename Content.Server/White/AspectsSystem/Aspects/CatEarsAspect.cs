using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.White.AspectsSystem.Aspects.Components;
using Content.Server.White.AspectsSystem.Base;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;

namespace Content.Server.White.AspectsSystem.Aspects;

public sealed class CatEarsAspect : AspectSystem<CatEarsAspectComponent>
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;

    private MarkingPrototype _ears = default!;
    private MarkingPrototype _tail = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(HandleLateJoin);

        _ears = _protoMan.Index<MarkingPrototype>("FelinidEarsBasic");
        _tail = _protoMan.Index<MarkingPrototype>("FelinidTailBasic");
    }

    protected override void Started(EntityUid uid, CatEarsAspectComponent component, GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);
        var query = EntityQueryEnumerator<HumanoidAppearanceComponent>();
        while (query.MoveNext(out var ent, out var appearance))
        {
            AddMarkings(ent, appearance);
        }
    }

    private void HandleLateJoin(PlayerSpawnCompleteEvent ev)
    {
        var query = EntityQueryEnumerator<RandomAppearanceAspectComponent, GameRuleComponent>();
        while (query.MoveNext(out var ruleEntity, out _, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(ruleEntity, gameRule))
                continue;

            if (!ev.LateJoin)
                return;

            AddMarkings(ev.Mob);
        }
    }

    private void AddMarkings(EntityUid uid, HumanoidAppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref appearance, false))
            return;

        switch (appearance.Species)
        {
            case "Felinid":
                return;
            case "Human":
            {
                if (!appearance.MarkingSet.TryGetCategory(MarkingCategories.HeadTop, out var markings) ||
                    markings.Count == 0)
                    AddEars(appearance);

                if (!appearance.MarkingSet.TryGetCategory(MarkingCategories.Tail, out markings) || markings.Count == 0)
                    AddTail(appearance);

                Dirty(uid, appearance);
                return;
            }
            default:
                AddEars(appearance);
                AddTail(appearance);
                Dirty(uid, appearance);
                break;
        }
    }

    private List<Color> GetColors(HumanoidAppearanceComponent appearance, MarkingPrototype prototype)
    {
        return MarkingColoring.GetMarkingLayerColors(prototype, appearance.SkinColor, appearance.EyeColor,
            appearance.MarkingSet);
    }

    private void AddTail(HumanoidAppearanceComponent appearance)
    {
        if (!appearance.MarkingSet.TryGetMarking(MarkingCategories.Tail, _tail.ID, out _))
        {
            appearance.MarkingSet.AddFront(MarkingCategories.Tail,
                new Marking(_tail.ID, GetColors(appearance, _tail)) {Forced = true});
        }
    }

    private void AddEars(HumanoidAppearanceComponent appearance)
    {
        if (!appearance.MarkingSet.TryGetMarking(MarkingCategories.HeadTop, _tail.ID, out _))
        {
            appearance.MarkingSet.AddFront(MarkingCategories.HeadTop,
                new Marking(_ears.ID, GetColors(appearance, _ears)) {Forced = true});
        }
    }
}
