using Content.Server.Power.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Events;
using Content.Shared.Interaction.Events;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Server.Audio;

namespace Content.Server.Weapons.Ranged.Systems;

public sealed partial class GunSystem
{
    [Dependency] private readonly AudioSystem _audio = default!;

    protected override void InitializeBattery()
    {
        base.InitializeBattery();

        // Hitscan
        SubscribeLocalEvent<HitscanBatteryAmmoProviderComponent, ComponentStartup>(OnBatteryStartup);
        SubscribeLocalEvent<HitscanBatteryAmmoProviderComponent, ChargeChangedEvent>(OnBatteryChargeChange);
        SubscribeLocalEvent<HitscanBatteryAmmoProviderComponent, DamageExamineEvent>(OnBatteryDamageExamine);

        // Projectile
        SubscribeLocalEvent<ProjectileBatteryAmmoProviderComponent, ComponentStartup>(OnBatteryStartup);
        SubscribeLocalEvent<ProjectileBatteryAmmoProviderComponent, ChargeChangedEvent>(OnBatteryChargeChange);
        SubscribeLocalEvent<ProjectileBatteryAmmoProviderComponent, DamageExamineEvent>(OnBatteryDamageExamine);

        //TwoModeEnergy
        SubscribeLocalEvent<TwoModeEnergyAmmoProviderComponent, ComponentStartup>(OnBatteryStartup);
        SubscribeLocalEvent<TwoModeEnergyAmmoProviderComponent, ChargeChangedEvent>(OnBatteryChargeChange);
        SubscribeLocalEvent<TwoModeEnergyAmmoProviderComponent, DamageExamineEvent>(OnBatteryDamageExamine);
        SubscribeLocalEvent<TwoModeEnergyAmmoProviderComponent, UseInHandEvent>(OnBatteryModeChange);
    }

    private void OnBatteryModeChange(EntityUid uid, TwoModeEnergyAmmoProviderComponent component, UseInHandEvent args)
    {
        if (!TryComp<GunComponent>(uid, out var gun))
            return;

        switch (component.CurrentMode)
        {
            case EnergyModes.Stun:
                component.InStun = false;
                component.CurrentMode = EnergyModes.Laser;
                component.FireCost = component.LaserFireCost;
                gun.SoundGunshot = component.LaserSound;
                gun.ProjectileSpeed = component.LaserProjectileSpeed;
                _audio.PlayPvs(component.ToggleSound, args.User);
                break;
            case EnergyModes.Laser:
                component.InStun = true;
                component.CurrentMode = EnergyModes.Stun;
                component.FireCost = component.StunFireCost;
                gun.SoundGunshot = component.StunSound;
                gun.ProjectileSpeed = component.StunProjectileSpeed;
                _audio.PlayPvs(component.ToggleSound, args.User);
                break;
        }

        UpdateShots(uid, component);
        UpdateTwoModeAppearance(uid, component);
        UpdateBatteryAppearance(uid, component);
        UpdateAmmoCount(uid);
    }

    private void OnBatteryStartup(EntityUid uid, BatteryAmmoProviderComponent component, ComponentStartup args)
    {
        UpdateShots(uid, component);
    }

    private void OnBatteryChargeChange(
        EntityUid uid,
        BatteryAmmoProviderComponent component,
        ref ChargeChangedEvent args)
    {
        UpdateShots(uid, component, args.Charge, args.MaxCharge);
    }

    private void UpdateShots(EntityUid uid, BatteryAmmoProviderComponent component)
    {
        if (!TryComp<BatteryComponent>(uid, out var battery))
            return;

        UpdateShots(uid, component, battery.Charge, battery.MaxCharge);
    }

    private void UpdateShots(EntityUid uid, BatteryAmmoProviderComponent component, float charge, float maxCharge)
    {
        var shots = (int) (charge / component.FireCost);
        var maxShots = (int) (maxCharge / component.FireCost);

        if (component.Shots != shots || component.Capacity != maxShots)
        {
            Dirty(uid, component);
        }

        component.Shots = shots;
        component.Capacity = maxShots;
        UpdateBatteryAppearance(uid, component);
    }

    private void OnBatteryDamageExamine(
        EntityUid uid,
        BatteryAmmoProviderComponent component,
        ref DamageExamineEvent args)
    {
        var damageSpec = GetDamage(component);

        if (damageSpec == null)
            return;

        var damageType = component switch
        {
            HitscanBatteryAmmoProviderComponent    => Loc.GetString("damage-hitscan"),
            ProjectileBatteryAmmoProviderComponent => Loc.GetString("damage-projectile"),
            TwoModeEnergyAmmoProviderComponent twoMode => Loc.GetString(twoMode.CurrentMode == EnergyModes.Stun
                ? "damage-projectile"
                : "damage-hitscan"),
            _ => throw new ArgumentOutOfRangeException()
        };

        _damageExamine.AddDamageExamine(args.Message, damageSpec, damageType);
    }

    private DamageSpecifier? GetDamage(BatteryAmmoProviderComponent component)
    {
        return component switch
        {
            HitscanBatteryAmmoProviderComponent hitscan =>
                ProtoManager.Index<HitscanPrototype>(hitscan.Prototype).Damage,
            ProjectileBatteryAmmoProviderComponent battery => GetProjectileDamage(battery.Prototype),
            TwoModeEnergyAmmoProviderComponent twoMode => GetProjectileDamage(twoMode.CurrentMode == EnergyModes.Laser
                ? twoMode.LaserPrototype
                : twoMode.StunPrototype),
            _ => null
        };
    }

    protected override void TakeCharge(EntityUid uid, BatteryAmmoProviderComponent component)
    {
        // Will raise ChargeChangedEvent
        _battery.UseCharge(uid, component.FireCost);
    }
}
