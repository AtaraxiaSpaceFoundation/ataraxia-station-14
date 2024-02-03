using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Hands;

namespace Content.Shared._White.ReduceBlindness;

public sealed class ReduceBlindnessSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReduceBlindnessComponent, GotEquippedHandEvent>(OnEquipepd);
        SubscribeLocalEvent<ReduceBlindnessComponent, GotUnequippedHandEvent>(OnUnequipped);
    }

    private void OnEquipepd(Entity<ReduceBlindnessComponent> ent, ref GotEquippedHandEvent args)
    {
        if (!TryComp(args.User, out BlurryVisionComponent? blurryVisionComponent))
        {
            return;
        }

        blurryVisionComponent.Magnitude -= ent.Comp.ReduceAmount;
    }

    private void OnUnequipped(Entity<ReduceBlindnessComponent> ent, ref GotUnequippedHandEvent args)
    {
        if (!TryComp(args.User, out BlurryVisionComponent? blurryVisionComponent))
        {
            return;
        }

        blurryVisionComponent.Magnitude += ent.Comp.ReduceAmount;
    }
}
