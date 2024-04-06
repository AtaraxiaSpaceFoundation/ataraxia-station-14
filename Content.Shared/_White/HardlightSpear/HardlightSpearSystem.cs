using System.Linq;
using Content.Shared.Body.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Implants.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Spawners;

namespace Content.Shared._White.HardlightSpear;

public sealed class HardlightSpearSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HardlightSpearComponent, LandEvent>(OnLand);
        SubscribeLocalEvent<HardlightSpearComponent, DroppedEvent>(OnDrop);
        SubscribeLocalEvent<HardlightSpearComponent, EntGotInsertedIntoContainerMessage>(OnInsert);
        SubscribeLocalEvent<HardlightSpearComponent, GettingPickedUpAttemptEvent>(OnPickupAttempt);
        SubscribeLocalEvent<HardlightSpearComponent, PreventCollideEvent>(OnPreventCollision);
        SubscribeLocalEvent<SubdermalImplantComponent, ActivateHardlightSpearImplantEvent>(OnImplantActivate);
        SubscribeLocalEvent<MobStateComponent, PreventCollideEvent>(OnMobStatePreventCollision);
    }

    private void OnMobStatePreventCollision(Entity<MobStateComponent> ent, ref PreventCollideEvent args)
    {
        if (ent.Comp.CurrentState == MobState.Dead && HasComp<EmbeddableProjectileComponent>(args.OtherEntity) &&
            HasComp<ThrownItemComponent>(args.OtherEntity))
            args.Cancelled = true;
    }

    private void OnPreventCollision(EntityUid uid, HardlightSpearComponent component, ref PreventCollideEvent args)
    {
        // Opaque collision mask doesn't work for EmbeddableProjectileComponent
        if (TryComp(args.OtherEntity, out FixturesComponent? fixtures) &&
            fixtures.Fixtures.All(fix => (fix.Value.CollisionLayer & (int) CollisionGroup.Opaque) == 0))
        {
            args.Cancelled = true;
        }
    }

    private void OnImplantActivate(EntityUid uid, SubdermalImplantComponent component,
        ActivateHardlightSpearImplantEvent args)
    {
        if (!TryComp(component.ImplantedEntity, out TransformComponent? transform))
            return;

        var spear = EntityManager.SpawnEntity("SpearHardlight", transform.Coordinates);

        if (_hands.TryPickupAnyHand(component.ImplantedEntity.Value, spear))
        {
            _audio.PlayPvs("/Audio/Weapons/ebladeon.ogg", spear);
            args.Handled = true;
            return;
        }

        Del(spear);
    }

    private void OnPickupAttempt(EntityUid uid, HardlightSpearComponent component, GettingPickedUpAttemptEvent args)
    {
        if (!HasComp<TimedDespawnComponent>(uid))
            return;

        args.Cancel();
        _popup.PopupClient(Loc.GetString("hardlight-spear-pickup-failed"), uid, args.User);
    }

    private void OnDrop(EntityUid uid, HardlightSpearComponent component, DroppedEvent args)
    {
        EnsureComp<TimedDespawnComponent>(uid);
    }

    private void OnLand(EntityUid uid, HardlightSpearComponent component, ref LandEvent args)
    {
        EnsureComp<TimedDespawnComponent>(uid);
    }

    private void OnInsert(EntityUid uid, HardlightSpearComponent component, EntGotInsertedIntoContainerMessage args)
    {
        if (!HasComp<BodyComponent>(args.Container.Owner))
            EnsureComp<TimedDespawnComponent>(uid);
    }
}
