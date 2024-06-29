using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Projectiles;
using Content.Shared.Standing.Systems;
using Content.Shared.StatusEffect;

namespace Content.Shared._White.Knockdown;

public sealed class KnockdownOnCollideSystem : EntitySystem
{
    [Dependency] private readonly SharedStandingStateSystem _standing = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KnockdownOnCollideComponent, ProjectileHitEvent>(OnProjectileHit);
    }

    private void OnProjectileHit(Entity<KnockdownOnCollideComponent> ent, ref ProjectileHitEvent args)
    {
        _standing.TryLieDown(args.Target, null, SharedStandingStateSystem.DropHeldItemsBehavior.AlwaysDrop);

        if (ent.Comp.BlurTime <= 0f)
            return;

        _statusEffects.TryAddStatusEffect<BlurryVisionComponent>(args.Target, "BlurryVision",
            TimeSpan.FromSeconds(ent.Comp.BlurTime), true);
    }
}
