using System.Numerics;
using Content.Shared.Camera;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Telescope;

public abstract class SharedTelescopeSystem : EntitySystem
{
    [Dependency] private readonly SharedEyeSystem _eye = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeAllEvent<EyeOffsetChangedEvent>(OnEyeOffsetChanged);
        SubscribeLocalEvent<TelescopeComponent, GotUnequippedHandEvent>(OnUnequip);
        SubscribeLocalEvent<TelescopeComponent, HandDeselectedEvent>(OnHandDeselected);
        SubscribeLocalEvent<TelescopeComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnShutdown(Entity<TelescopeComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp(ent.Comp.LastHoldingEntity, out EyeComponent? eye))
            return;

        SetOffset((ent.Comp.LastHoldingEntity.Value, eye), Vector2.Zero, ent);
    }

    private void OnHandDeselected(Entity<TelescopeComponent> ent, ref HandDeselectedEvent args)
    {
        if (!TryComp(args.User, out EyeComponent? eye))
            return;

        SetOffset((args.User, eye), Vector2.Zero, ent);
    }

    private void OnUnequip(Entity<TelescopeComponent> ent, ref GotUnequippedHandEvent args)
    {
        if (!TryComp(args.User, out EyeComponent? eye))
            return;

        SetOffset((args.User, eye), Vector2.Zero, ent);
    }

    private void OnEyeOffsetChanged(EyeOffsetChangedEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } ent)
            return;

        if (!TryComp<HandsComponent>(ent, out var hands) ||
            !TryComp<TelescopeComponent>(hands.ActiveHandEntity, out var telescope) ||
            !TryComp(ent, out EyeComponent? eye))
            return;

        var offset = Vector2.Lerp(eye.Offset, msg.Offset, telescope.LerpAmount);

        SetOffset((ent, eye), offset, telescope);
    }

    private void SetOffset(Entity<EyeComponent> ent, Vector2 offset, TelescopeComponent telescope)
    {
        telescope.LastHoldingEntity = ent;

        if (TryComp(ent, out CameraRecoilComponent? recoil))
        {
            recoil.BaseOffset = offset;
            _eye.SetOffset(ent, offset + recoil.CurrentKick, ent);
        }
        else
            _eye.SetOffset(ent, offset, ent);
    }
}

[Serializable, NetSerializable]
public sealed class EyeOffsetChangedEvent : EntityEventArgs
{
    public Vector2 Offset;
}
