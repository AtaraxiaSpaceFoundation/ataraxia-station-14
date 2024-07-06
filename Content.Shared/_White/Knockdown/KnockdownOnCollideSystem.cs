using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Projectiles;
using Content.Shared.Standing.Systems;
using Content.Shared.StatusEffect;
using Content.Shared.Throwing;

namespace Content.Shared._White.Knockdown;

public sealed class KnockdownOnCollideSystem : EntitySystem
{
    [Dependency] private readonly SharedStandingStateSystem _standing = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KnockdownOnCollideComponent, ProjectileHitEvent>(OnProjectileHit);
        SubscribeLocalEvent<KnockdownOnCollideComponent, ThrowDoHitEvent>(OnEntityHit);
    }

    private void OnEntityHit(Entity<KnockdownOnCollideComponent> ent, ref ThrowDoHitEvent args)
    {
        ApplyEffects(args.Target, ent.Comp);
    }

    private void OnProjectileHit(Entity<KnockdownOnCollideComponent> ent, ref ProjectileHitEvent args)
    {
        ApplyEffects(args.Target, ent.Comp);
    }

    private void ApplyEffects(EntityUid target, KnockdownOnCollideComponent component)
    {
        _standing.TryLieDown(target, null, SharedStandingStateSystem.DropHeldItemsBehavior.AlwaysDrop);

        if (component.UseBlur)
            _statusEffects.TryAddStatusEffect<BlurryVisionComponent>(target, "BlurryVision", TimeSpan.FromSeconds(component.BlurTime), true);
    }
}
