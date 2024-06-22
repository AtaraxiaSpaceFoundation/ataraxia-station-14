using Content.Server.Singularity.EntitySystems;

namespace Content.Server._White.Wizard.GravPulseOnStartup;

public sealed class GravPulseOnStartupSystem : EntitySystem
{
    [Dependency] private readonly GravityWellSystem _gravityWell = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GravPulseOnStartupComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<GravPulseOnStartupComponent> ent, ref ComponentStartup args)
    {
        var (uid, comp) = ent;
        _gravityWell.GravPulse(Transform(uid).Coordinates, comp.MaxRange, comp.MinRange, comp.BaseRadialAcceleration,
            comp.BaseTangentialAcceleration, comp.StunTime);
    }
}
