using System.Diagnostics.CodeAnalysis;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server._White.Flamethrower;
using Content.Shared._White.Flamethrower;
using Robust.Shared.Containers;

namespace Content.Server.Weapons.Ranged.Systems;

public sealed partial class GunSystem
{
    [Dependency] private readonly GasTankSystem _gasTank = default!;

    private const string GasTankSlot = "gas_tank";

    protected override void InitializeGas()
    {
        base.InitializeGas();

        SubscribeLocalEvent<GasAmmoProviderComponent, ComponentStartup>(OnGasStartup);
        SubscribeLocalEvent<GasAmmoProviderComponent, EntInsertedIntoContainerMessage>(OnGasSlotChange);
        SubscribeLocalEvent<GasAmmoProviderComponent, EntRemovedFromContainerMessage>(OnGasSlotChange);
    }

    private void OnGasSlotChange(EntityUid uid, GasAmmoProviderComponent component, ContainerModifiedMessage args)
    {
        if (GasTankSlot != args.Container.ID)
            return;

        UpdateShots(uid, component);
    }

    private void OnGasStartup(EntityUid uid, GasAmmoProviderComponent component, ComponentStartup args)
    {
        UpdateShots(uid, component);
    }

    private static int CalculateShots(GasAmmoProviderComponent component, GasTankComponent tank)
    {
        return (int) MathF.Ceiling(tank.Air.TotalMoles / component.GasUsage);
    }

    private void UpdateShots(EntityUid uid, GasAmmoProviderComponent component)
    {
        var shots = 0;
        var pressure = 0f;

        if (TryTakeGasTankComponent(uid, out var tank, out _))
        {
            shots = CalculateShots(component, tank);
            pressure = tank.Air.Pressure;
        }

        UpdateShots(component, shots, pressure);
    }

    private void UpdateShots(GasAmmoProviderComponent component, int shots, float pressure)
    {
        if (component.Shots != shots || MathF.Abs(component.Pressure - pressure) > 1e-3f)
        {
            Dirty(component);
        }

        component.Shots = shots;
        component.Pressure = pressure;
    }

    private bool TryTakeGasTankComponent(EntityUid uid, [NotNullWhen(true)] out GasTankComponent? tank,
        [NotNullWhen(true)] out EntityUid? tankUid)
    {
        if (!Containers.TryGetContainer(uid, GasTankSlot, out var container) ||
            container is not ContainerSlot slot)
        {
            tank = null;
            tankUid = null;
            return false;
        }

        tankUid = slot.ContainedEntity;

        if (tankUid != null)
            return TryComp(tankUid, out tank);

        tank = null;
        return false;

    }

    protected override void InitShot(EntityUid uid, GasAmmoProviderComponent component, EntityUid shotUid)
    {
        if (!TryTakeGasTankComponent(uid, out var tank, out var tankUid) ||
            !TryComp(shotUid, out GasProjectileComponent? proj))
            return;

        var trans = Transform(uid);

        var curTile = TransformSystem.GetGridOrMapTilePosition(uid, trans);

        var tileInfo = new TileInfo(trans.GridUid, trans.MapUid, curTile);
        proj.LastTile = tileInfo;
        proj.CurTile = tileInfo;

        var removed = _gasTank.RemoveAir((tankUid.Value, tank), component.GasUsage);

        if(removed is not null)
            proj.GasMixture = removed;

        UpdateShots(component, CalculateShots(component, tank), tank.Air.Pressure);
    }
}
