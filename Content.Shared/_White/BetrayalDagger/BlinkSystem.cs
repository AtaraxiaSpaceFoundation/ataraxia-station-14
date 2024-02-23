using System.Numerics;
using Content.Shared.Mobs.Components;
using Content.Shared.Physics;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._White.BetrayalDagger;

public sealed class BlinkSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeAllEvent<BlinkEvent>(OnBlink);
    }

    private void OnBlink(BlinkEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity == null)
            return;

        var user = args.SenderSession.AttachedEntity.Value;

        if (!TryComp(user, out TransformComponent? xform))
            return;

        if (!TryComp(GetEntity(msg.Weapon), out BlinkComponent? blink))
            return;

        if (blink.NextBlink > _timing.CurTime)
            return;

        var blinkRate = TimeSpan.FromSeconds(1f / blink.BlinkRate);

        blink.NextBlink = _timing.CurTime + blinkRate;

        var coords = _transform.GetWorldPosition(xform);
        var dir = msg.Direction.Normalized();
        var range = blink.Distance;

        var ray = new CollisionRay(coords, dir, (int) CollisionGroup.Opaque);
        var rayResults = _physics.IntersectRayWithPredicate(xform.MapID, ray, range,
            x => x == user || !HasComp<OccluderComponent>(x)).FirstOrNull();

        Vector2 targetPos;
        if (rayResults != null)
        {
            targetPos = rayResults.Value.HitPos - dir;
        }
        else
        {
            targetPos = coords + (msg.Direction.Length() > range ? dir * range : msg.Direction);
        }

        _transform.SetWorldPosition(user, targetPos);
        _audio.PlayPvs(blink.BlinkSound, user);
    }
}

[Serializable, NetSerializable]
public sealed class BlinkEvent : EntityEventArgs
{
    public readonly NetEntity Weapon;
    public readonly Vector2 Direction;

    public BlinkEvent(NetEntity weapon, Vector2 direction)
    {
        Weapon = weapon;
        Direction = direction;
    }
}
