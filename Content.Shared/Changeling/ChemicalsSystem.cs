using Content.Shared.Alert;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Network;

namespace Content.Shared.Changeling;

public sealed class ChemicalsSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;
    [Dependency] private readonly INetManager _net = default!;

    public bool AddChemicals(EntityUid uid, ChangelingComponent component, int quantity)
    {
        if (_mobStateSystem.IsDead(uid))
            return false;

        var toAdd = quantity;

        if (component.ChemicalsBalance == component.ChemicalCapacity)
            return false;

        if (component.ChemicalsBalance + toAdd > component.ChemicalCapacity)
        {
            var overflow = component.ChemicalsBalance + toAdd - component.ChemicalCapacity;
            toAdd -= overflow;
            component.ChemicalsBalance += toAdd;
        }

        component.ChemicalsBalance += toAdd;
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

            if (component.IsRegenerating)
                continue;

            component.Accumulator = 0;
            AddChemicals(uid, component, component.ChemicalRegenRate);
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
