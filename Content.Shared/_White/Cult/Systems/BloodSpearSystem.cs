using Content.Shared._White.Cult.Components;
using Content.Shared.Actions;
using Content.Shared.Hands;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;

namespace Content.Shared._White.Cult.Systems;

public sealed class BloodSpearSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodSpearComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<BloodSpearComponent, GotEquippedHandEvent>(OnEquip);
        SubscribeLocalEvent<BloodSpearComponent, ThrowDoHitEvent>(OnThrowDoHit);
    }

    private void OnThrowDoHit(Entity<BloodSpearComponent> ent, ref ThrowDoHitEvent args)
    {
        if (!TryComp(args.Target, out StatusEffectsComponent? status))
            return;

        if(!_stunSystem.TryParalyze(args.Target, TimeSpan.FromSeconds(6), true, status))
            return;

        if (_net.IsClient)
            return;

        _audio.PlayPvs(ent.Comp.ShatterSound, Transform(ent).Coordinates);
        QueueDel(ent);
    }

    private void OnEquip(Entity<BloodSpearComponent> ent, ref GotEquippedHandEvent args)
    {
        if (!TryComp(args.User, out CultistComponent? cultist))
            return;

        Entity<CultistComponent> user = (args.User, cultist);

        if (cultist.BloodSpear == ent && ent.Comp.User == user)
            return;

        if (ent.Comp.User != null)
            DetachSpearFromUser(ent.Comp.User.Value);
        DetachSpearFromUser(user);
        AttachSpearToUser(user, ent);
    }

    public void DetachSpearFromUser(Entity<CultistComponent> user)
    {
        _actionsSystem.RemoveAction(user, user.Comp.BloodSpearActionEntity);
        user.Comp.BloodSpearActionEntity = null;
        if (user.Comp.BloodSpear != null)
            user.Comp.BloodSpear.Value.Comp.User = null;
        user.Comp.BloodSpear = null;
    }

    public void AttachSpearToUser(Entity<CultistComponent> user, Entity<BloodSpearComponent> spear)
    {
        _actionsSystem.AddAction(user, ref user.Comp.BloodSpearActionEntity, spear.Comp.Action);
        user.Comp.BloodSpear = spear;
        spear.Comp.User = user;
    }

    private void OnRemove(Entity<BloodSpearComponent> ent, ref ComponentRemove args)
    {
        if (ent.Comp.User != null)
            DetachSpearFromUser(ent.Comp.User.Value);
    }
}
