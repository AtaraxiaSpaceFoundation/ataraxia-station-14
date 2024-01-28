using System.Linq;
using Content.Server.Stunnable;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Robust.Server.Audio;
using Robust.Server.GameObjects;

namespace Content.Server._White.Cult.HolyWater;

public sealed class HolyWaterSystem : EntitySystem
{
    [Dependency] private readonly StunSystem _stun = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly AudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BibleWaterConvertComponent, AfterInteractEvent>(OnBibleInteract);
    }

    private void OnBibleInteract(EntityUid uid, BibleWaterConvertComponent component, AfterInteractEvent args)
    {
        if (HasComp<MobStateComponent>(uid))
            return;

        if (!TryComp<SolutionContainerManagerComponent>(args.Target, out var container))
            return;

        if (container.Solutions != null)
        {
            foreach (var solution in container.Solutions.Values.Where(solution =>
                         solution.ContainsReagent(component.ConvertedId, null)))
            {
                foreach (var reagent in solution.Contents)
                {
                    if (reagent.Reagent.Prototype != component.ConvertedId)
                        continue;

                    var amount = reagent.Quantity;

                    solution.RemoveReagent(reagent.Reagent.Prototype, reagent.Quantity);
                    solution.AddReagent(component.ConvertedToId, amount);

                    if (args.Target == null)
                        return;

                    _popup.PopupEntity(Loc.GetString("holy-water-converted"), args.Target.Value, args.User);
                    _audio.PlayPvs("/Audio/Effects/holy.ogg", args.Target.Value);

                    return;
                }
            }
        }
    }
}
