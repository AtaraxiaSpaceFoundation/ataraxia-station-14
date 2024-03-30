using Content.Server.Stunnable.Components;
using Content.Shared._White.Implants.NeuroControl;
using Content.Shared.Standing;
using Content.Shared.StatusEffect;
using JetBrains.Annotations;
using Robust.Shared.Physics.Dynamics;
using Content.Shared.Throwing;
using Robust.Shared.Physics.Events;

namespace Content.Server.Stunnable
{
    [UsedImplicitly]
    internal sealed class StunOnCollideSystem : EntitySystem
    {
        [Dependency] private readonly StunSystem _stunSystem = default!;
        [Dependency] private readonly NeuroControlSystem _neuroControl = default!; // WD EDIT

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<StunOnCollideComponent, StartCollideEvent>(HandleCollide);
            SubscribeLocalEvent<StunOnCollideComponent, ThrowDoHitEvent>(HandleThrow);
        }

        private void TryDoCollideStun(EntityUid uid, StunOnCollideComponent component, EntityUid target)
        {
            // WD START
            var neuroControlled = HasComp<NeuroControlComponent>(target);
            var stunAmount = component.StunAmount;
            var knockdownAmount = component.KnockdownAmount;
            if (neuroControlled)
            {
                stunAmount = Math.Max(1, stunAmount / 6);
                knockdownAmount = Math.Max(1, knockdownAmount / 6);
            }
            // WD END

            if (EntityManager.TryGetComponent<StatusEffectsComponent>(target, out var status))
            {
                // WD EDIT START
                _stunSystem.TryStun(target, TimeSpan.FromSeconds(stunAmount), true, status);

                _stunSystem.TryKnockdown(target, TimeSpan.FromSeconds(knockdownAmount), true,
                    status);

                if (neuroControlled)
                {
                    _neuroControl.Electrocute(target, component.StunAmount * 6, status);
                    return;
                }
                // WD EDIT END

                _stunSystem.TrySlowdown(target, TimeSpan.FromSeconds(component.SlowdownAmount), true,
                    component.WalkSpeedMultiplier, component.RunSpeedMultiplier, status);
            }
        }
        private void HandleCollide(EntityUid uid, StunOnCollideComponent component, ref StartCollideEvent args)
        {
            if (args.OurFixtureId != component.FixtureID)
                return;

            TryDoCollideStun(uid, component, args.OtherEntity);
        }

        private void HandleThrow(EntityUid uid, StunOnCollideComponent component, ThrowDoHitEvent args)
        {
            TryDoCollideStun(uid, component, args.Target);
        }
    }
}
