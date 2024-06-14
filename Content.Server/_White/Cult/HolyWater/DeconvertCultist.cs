using System.Threading;
using Content.Server._White.Cult.GameRule;
using Content.Server.Objectives.Components;
using Content.Server.Popups;
using Content.Server.Roles;
using Content.Server.Stunnable;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared._White.Cult.Components;
using Content.Shared._White.Mood;
using Content.Shared.Jittering;
using Content.Shared.Mind;
using JetBrains.Annotations;
using Robust.Server.Containers;
using Robust.Shared.Prototypes;
using CultistComponent = Content.Shared._White.Cult.Components.CultistComponent;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server._White.Cult.HolyWater;

[ImplicitDataDefinitionForInheritors]
[MeansImplicitUse]
public sealed partial class DeconvertCultist : ReagentEffect
{
    public override bool ShouldLog => true;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("reagent-effect-guidebook-deconvert-cultist");
    }

    public override void Effect(ReagentEffectArgs args)
    {
        var uid = args.SolutionEntity;

        if (!args.EntityManager.TryGetComponent(uid, out CultistComponent? component))
            return;

        if (component.HolyConvertToken != null)
            return;

        args.EntityManager.System<StunSystem>()
            .TryParalyze(uid, TimeSpan.FromSeconds(component.HolyConvertTime + 5f), true);
        args.EntityManager.System<SharedJitteringSystem>()
            .DoJitter(uid, TimeSpan.FromSeconds(component.HolyConvertTime + 5f), true);
        var target = Identity.Name(uid, args.EntityManager);
        args.EntityManager.System<PopupSystem>()
            .PopupEntity(Loc.GetString("holy-water-started-converting", ("target", target)), uid);

        component.HolyConvertToken = new CancellationTokenSource();
        Timer.Spawn(TimeSpan.FromSeconds(component.HolyConvertTime), () => ConvertCultist(uid, args.EntityManager),
            component.HolyConvertToken.Token);
    }

    private void ConvertCultist(EntityUid uid, IEntityManager entityManager)
    {
        if (!entityManager.TryGetComponent<CultistComponent>(uid, out var cultist))
            return;

        cultist.HolyConvertToken = null;

        var inventory = entityManager.System<InventorySystem>();
        var containerSystem = entityManager.System<ContainerSystem>();
        if (!inventory.TryGetContainerSlotEnumerator(uid, out var enumerator))
            return;

        while (enumerator.MoveNext(out var container))
        {
            if (container.ContainedEntity != null &&
                entityManager.HasComponent<CultItemComponent>(container.ContainedEntity.Value))
            {
                containerSystem.Remove(container.ContainedEntity.Value, container, true, true);
            }
        }

        entityManager.RemoveComponent<CultistComponent>(uid);
        entityManager.RemoveComponent<PentagramComponent>(uid);

        var cultRuleSystem = entityManager.System<CultRuleSystem>();
        cultRuleSystem.RemoveObjectiveAndRole(uid);

        entityManager.EventBus.RaiseLocalEvent(uid, new MoodRemoveEffectEvent("CultFocused"));
    }
}
