using Content.Server.Lightning;
using Content.Shared.Projectiles;

namespace Content.Server._White.Wizard.Magic.TeslaProjectile;

public sealed class TeslaProjectileSystem : EntitySystem
{
    [Dependency] private readonly LightningSystem _lightning = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TeslaProjectileComponent, ProjectileHitEvent>(OnStartCollide);
    }

    private void OnStartCollide(Entity<TeslaProjectileComponent> ent, ref ProjectileHitEvent args)
    {
       _lightning.ShootRandomLightnings(ent, 2, 4, arcDepth:2);
    }
}
