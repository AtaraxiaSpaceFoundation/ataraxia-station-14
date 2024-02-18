using Content.Shared.Inventory;
using Content.Shared.Temperature;

namespace Content.Server._White.ChangeTemperatureOnCollide;

public sealed class ClothingTemperatureAdjustSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClothingTemperatureAdjustComponent, InventoryRelayedEvent<AdjustTemperatureEvent>>(
            OnAdjustTemperature);
    }

    private void OnAdjustTemperature(Entity<ClothingTemperatureAdjustComponent> ent,
        ref InventoryRelayedEvent<AdjustTemperatureEvent> args)
    {
        var delta = ent.Comp.TargetTemperature - args.Args.Temperature;
        var rate = Math.Min(ent.Comp.Rate, Math.Abs(delta));
        args.Args.Temperature += Math.Sign(delta) * rate;
    }
}
