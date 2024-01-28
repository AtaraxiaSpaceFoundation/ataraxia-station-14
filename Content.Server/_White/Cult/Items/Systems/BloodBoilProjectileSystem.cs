using Content.Server._White.Cult.Items.Components;
using Content.Server._White.Cult.TimedProduction;
using Content.Shared._White.Cult;
using Content.Shared._White.Cult.Pylon;
using Robust.Shared.Physics.Events;
using CultistComponent = Content.Shared._White.Cult.Components.CultistComponent;

namespace Content.Server._White.Cult.Items.Systems;

public sealed class BloodBoilProjectileSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodBoilProjectileComponent, PreventCollideEvent>(PreventCollision);
    }

    private void PreventCollision(EntityUid uid, BloodBoilProjectileComponent component, ref PreventCollideEvent args)
    {
        if (HasComp<CultistComponent>(args.OtherEntity) || HasComp<ConstructComponent>(args.OtherEntity) ||
            HasComp<CultistFactoryComponent>(args.OtherEntity) || HasComp<SharedPylonComponent>(args.OtherEntity))
        {
            args.Cancelled = true;
        }
    }
}
