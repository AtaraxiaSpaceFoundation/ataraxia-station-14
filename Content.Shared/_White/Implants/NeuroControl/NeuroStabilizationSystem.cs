using Content.Shared.Damage.Systems;
using Content.Shared.Electrocution;
using Content.Shared.StatusEffect;

namespace Content.Shared._White.Implants.NeuroControl;

public sealed class NeuroStabilizationSystem : EntitySystem
{
    [Dependency] private readonly SharedElectrocutionSystem _electrocution = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NeuroStabilizationComponent, BeforeStaminaDamageEvent>(BeforeStaminaDamage);
    }

    private void BeforeStaminaDamage(Entity<NeuroStabilizationComponent> ent, ref BeforeStaminaDamageEvent args)
    {
        args.Cancelled = true;
        Electrocute(ent, (int) MathF.Round(args.Value * 2f / 4f));
    }

    public void Electrocute(EntityUid uid, int damage, StatusEffectsComponent? status = null)
    {
        _electrocution.TryDoElectrocution(uid, null, damage, TimeSpan.FromSeconds(1), false, 0.5f, status, true);
    }
}
