using Content.Server.Atmos.Components;
using Content.Server.Lightning;
using Content.Server.Temperature.Components;
using Content.Server.Temperature.Systems;
using Content.Shared._White.Wizard.SpellBlade;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Timing;

namespace Content.Server._White.Wizard.SpellBlade;

public sealed class SpellBladeSystem : SharedSpellBladeSystem
{
    [Dependency] private readonly TemperatureSystem _temperature = default!;
    [Dependency] private readonly LightningSystem _lightning = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FrostAspectComponent, MeleeHitEvent>(OnFrostMeleeHit);
        SubscribeLocalEvent<LightningAspectComponent, MeleeHitEvent>(OnLightningMeleeHit);
    }

    private void OnLightningMeleeHit(Entity<LightningAspectComponent> ent, ref MeleeHitEvent args)
    {
        if (args.Direction != null || args.HitEntities.Count != 1)
            return;

        if (ent.Comp.NextShock > _timing.CurTime)
            return;

        ent.Comp.NextShock = _timing.CurTime + ent.Comp.ShockRate;

        _lightning.ShootRandomLightnings(args.HitEntities[0], ent.Comp.Range, ent.Comp.BoltCount,
            ent.Comp.LightningPrototype, ent.Comp.ArcDepth, false, args.User);
    }

    private void OnFrostMeleeHit(Entity<FrostAspectComponent> ent, ref MeleeHitEvent args)
    {
        var temp = ent.Comp.TemperatureOnHit;
        if (args.Direction != null) // Heavy attack
            temp *= 0.5f;

        foreach (var entity in args.HitEntities)
        {
            if (!TryComp<TemperatureComponent>(entity, out var temperature))
                continue;

            var curTemp = temperature.CurrentTemperature;
            var newTemp = curTemp - temp;

            newTemp = curTemp < ent.Comp.MinTemperature
                ? MathF.Min(curTemp, newTemp)
                : Math.Max(newTemp, ent.Comp.MinTemperature);

            _temperature.ForceChangeTemperature(entity, newTemp, temperature);
        }
    }

    protected override void ApplyFireAspect(EntityUid uid)
    {
        var ignite = EnsureComp<IgniteOnMeleeHitComponent>(uid);
        ignite.FireStacks = 2f;
        EnsureComp<FireAspectComponent>(uid);
    }

    protected override void ApplyFrostAspect(EntityUid uid)
    {
        var ignite = EnsureComp<IgniteOnMeleeHitComponent>(uid);
        ignite.FireStacks = -5f;
        EnsureComp<FrostAspectComponent>(uid);
    }

    protected override void ApplyLightningAspect(EntityUid uid)
    {
        EnsureComp<LightningAspectComponent>(uid);
    }
}
