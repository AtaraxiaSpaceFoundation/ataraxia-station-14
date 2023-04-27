using Content.Server.Power.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Events;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction.Events;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Weapons.Ranged.Systems;

public sealed partial class GunSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;

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
        SubscribeLocalEvent<ProjectileBatteryAmmoProviderComponent, DamageExamineEvent>(OnBatteryDamageExamine);
        SubscribeLocalEvent<TwoModeEnergyAmmoProviderComponent, UseInHandEvent>(OnBatteryModeChange);
    }

    private void OnBatteryModeChange(EntityUid uid, TwoModeEnergyAmmoProviderComponent component, UseInHandEvent args)
    {
        if (!TryComp<GunComponent>(uid, out var gun))
            return;

        if (component.CurrentMode == EnergyModes.Stun)
        {
            component.InStun = false;
            gun.SoundGunshot = component.HitscanSound;
            component.CurrentMode = EnergyModes.Laser;
            component.FireCost = component.HitscanFireCost;
            _audio.PlayPvs(component.ToggleSound, args.User);
        }
        else if (component.CurrentMode == EnergyModes.Laser)
        {
            component.InStun = true;
            gun.SoundGunshot = component.ProjSound;
            component.CurrentMode = EnergyModes.Stun;
            component.FireCost = component.ProjFireCost;
            _audio.PlayPvs(component.ToggleSound, args.User);
        }
        UpdateShots(uid, component);
        UpdateTwoModeAppearance(uid, component);
        UpdateBatteryAppearance(uid, component);
        UpdateAmmoCount(uid);
        Dirty(gun);
        Dirty(component);
    }

    private void OnBatteryStartup(EntityUid uid, BatteryAmmoProviderComponent component, ComponentStartup args)
    {
        UpdateShots(uid, component);
    }

    private void OnBatteryChargeChange(EntityUid uid, BatteryAmmoProviderComponent component, ref ChargeChangedEvent args)
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
            Dirty(component);
        }

        component.Shots = shots;
        component.Capacity = maxShots;
        UpdateBatteryAppearance(uid, component);
    }

    private void OnBatteryDamageExamine(EntityUid uid, BatteryAmmoProviderComponent component, ref DamageExamineEvent args)
    {
        var damageSpec = GetDamage(component);

        if (damageSpec == null)
            return;

        string? damageType;
        switch (component)
        {
            case HitscanBatteryAmmoProviderComponent:
                damageType = Loc.GetString("damage-hitscan");
                break;
            case ProjectileBatteryAmmoProviderComponent:
                damageType = Loc.GetString("damage-projectile");
                break;
            case TwoModeEnergyAmmoProviderComponent twoMode:
                if (twoMode.CurrentMode == EnergyModes.Stun)
                    damageType = Loc.GetString("damage-projectile");
                else
                    damageType = Loc.GetString("damage-hitscan");
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        _damageExamine.AddDamageExamine(args.Message, damageSpec, damageType);
    }

    private DamageSpecifier? GetDamage(BatteryAmmoProviderComponent component)
    {
        if (component is ProjectileBatteryAmmoProviderComponent battery)
        {
            if (ProtoManager.Index<EntityPrototype>(battery.Prototype).Components
                .TryGetValue(_factory.GetComponentName(typeof(ProjectileComponent)), out var projectile))
            {
                var p = (ProjectileComponent) projectile.Component;

                if (!p.Damage.Empty)
                {
                    return p.Damage;
                }
            }

            return null;
        }

        if (component is HitscanBatteryAmmoProviderComponent hitscan)
        {
            return ProtoManager.Index<HitscanPrototype>(hitscan.Prototype).Damage;
        }

        if (component is TwoModeEnergyAmmoProviderComponent twoMode)
        {
            if (twoMode.CurrentMode == EnergyModes.Stun)
            {
                if (ProtoManager.Index<EntityPrototype>(twoMode.ProjectilePrototype).Components
                    .TryGetValue(_factory.GetComponentName(typeof(ProjectileComponent)), out var projectile))
                {
                    var p = (ProjectileComponent) projectile.Component;

                    if (p.Damage.Total > FixedPoint2.Zero)
                    {
                        return p.Damage;
                    }
                }

                return null;
            }
            return ProtoManager.Index<HitscanPrototype>(twoMode.HitscanPrototype).Damage;
        }
        return null;
    }

    protected override void TakeCharge(EntityUid uid, BatteryAmmoProviderComponent component)
    {
        // Will raise ChargeChangedEvent
        _battery.UseCharge(uid, component.FireCost);
    }
}
