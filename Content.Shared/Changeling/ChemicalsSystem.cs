using Content.Shared.Alert;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Network;

namespace Content.Shared.Changeling;

public sealed class ChemicalsSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClothingModifyChemicalRegenComponent, InventoryRelayedEvent<ChemRegenModifyEvent>>(
            OnChemRegenModify);
        SubscribeLocalEvent<VoidAdaptationComponent, ChemRegenModifyEvent>(OnVoidAdaptationChemRegenModify);
    }

    private void OnVoidAdaptationChemRegenModify(Entity<VoidAdaptationComponent> ent, ref ChemRegenModifyEvent args)
    {
        args.Multiplier *= ent.Comp.ChemMultiplier;
        ent.Comp.ChemMultiplier = 1f;
    }

    private void OnChemRegenModify(Entity<ClothingModifyChemicalRegenComponent> ent,
        ref InventoryRelayedEvent<ChemRegenModifyEvent> args)
    {
        args.Args.Multiplier *= ent.Comp.Multiplier;
    }

    public bool AddChemicals(EntityUid uid, ChangelingComponent component, int quantity)
    {
        var capacity = component.ChemicalCapacity;
        if (_mobStateSystem.IsDead(uid))
            capacity /= 2;

        if (component.ChemicalsBalance >= capacity)
            return false;

        component.ChemicalsBalance = Math.Min(component.ChemicalsBalance + quantity, capacity);

        Dirty(uid, component);

        UpdateAlert(uid, component);

        return true;
    }

    public bool RemoveChemicals(EntityUid uid, ChangelingComponent component, int quantity)
    {
        if (_mobStateSystem.IsDead(uid) && !component.IsRegenerating)
            return false;

        var toRemove = quantity;

        if (component.ChemicalsBalance == 0)
            return false;

        if (component.ChemicalsBalance - toRemove < 0)
            return false;

        component.ChemicalsBalance -= toRemove;
        Dirty(uid, component);

        UpdateAlert(uid, component);

        return true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ChangelingComponent>();

        while (query.MoveNext(out var uid, out var component))
        {
            component.Accumulator += frameTime;

            if(component.Accumulator < component.UpdateDelay)
                continue;

            component.Accumulator = 0;
            var ev = new ChemRegenModifyEvent();
            RaiseLocalEvent(uid, ev);
            var chemicals = (int) MathF.Round(component.ChemicalRegenRate * ev.Multiplier);
            AddChemicals(uid, component, chemicals);
        }
    }

    public void UpdateAlert(EntityUid uid, ChangelingComponent component)
    {
        if(_net.IsServer)
        {
            _alertsSystem.ShowAlert(uid, AlertType.Chemicals,
                (short) Math.Clamp(Math.Round(component.ChemicalsBalance / 10f), 0, 7));
        }
    }
}
