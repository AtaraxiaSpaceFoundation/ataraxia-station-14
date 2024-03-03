using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Chemistry;

[UsedImplicitly]
public sealed partial class HasComponent : ReagentEffectCondition
{
    [DataField]
    public string Component = default!;

    public override bool Condition(ReagentEffectArgs args)
    {
        return args.EntityManager.HasComponent(args.SolutionEntity,
            args.EntityManager.ComponentFactory.GetRegistration(Component).Type);
    }

    public override string GuidebookExplanation(IPrototypeManager prototype)
    {
        return string.Empty;
    }
}
