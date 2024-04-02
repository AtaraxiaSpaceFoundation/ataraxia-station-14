using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.Revenant.Components;

[UsedImplicitly]
public sealed partial class CureBlight : ReagentEffect
{
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return null;
    }

    public override void Effect(ReagentEffectArgs args)
    {
        args.EntityManager.RemoveComponentDeferred<BlightComponent>(args.SolutionEntity);
    }
}
