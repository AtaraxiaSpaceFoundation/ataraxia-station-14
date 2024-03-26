using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;

namespace Content.Shared.Fluids;

public abstract partial class SharedPuddleSystem
{
    [ValidatePrototypeId<ReagentPrototype>]
    private const string Water = "Water";

    [ValidatePrototypeId<ReagentPrototype>]
    private const string HolyWater = "Holywater";

    public static readonly string[] EvaporationReagents = [Water, HolyWater];

    public bool CanFullyEvaporate(Solution solution)
    {
        return solution.GetTotalPrototypeQuantity(EvaporationReagents) == solution.Volume;
    }
}