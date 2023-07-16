using Content.Server.Ghost.Components;
using Content.Shared.Eye;
using Content.Shared.Follower;
using Content.Shared.Ghost;
using Content.Shared.White.Administration;
using Robust.Server.GameObjects;

namespace Content.Server.White.Administration;

public sealed class InvisibilitySystem : SharedInvisibilitySystem
{
    [Dependency] private readonly VisibilitySystem _visibilitySystem = default!;
    [Dependency] private readonly FollowerSystem _followerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InvisibilityComponent, ComponentStartup>(OnInvisibilityStartup);
        SubscribeLocalEvent<InvisibilityComponent, ComponentShutdown>(OnInvisibilityShutdown);
    }

    private void OnInvisibilityStartup(EntityUid uid, InvisibilityComponent component, ComponentStartup args)
    {
        if (EntityManager.TryGetComponent(uid, out EyeComponent? eye))
        {
            eye.VisibilityMask |= (int) VisibilityFlags.AdminInvisible;
        }
    }

    private void OnInvisibilityShutdown(EntityUid uid, InvisibilityComponent component, ComponentShutdown args)
    {
        if (EntityManager.TryGetComponent(uid, out VisibilityComponent? visibility))
        {
            _visibilitySystem.RemoveLayer((uid, visibility), (int) VisibilityFlags.AdminInvisible);
        }

        if (EntityManager.TryGetComponent(uid, out EyeComponent? eye))
        {
            eye.VisibilityMask &= ~(int) VisibilityFlags.AdminInvisible;
        }
    }

    public void ToggleInvisibility(EntityUid uid, InvisibilityComponent component)
    {
        if (!EntityManager.TryGetComponent(uid, out VisibilityComponent? visibility))
            return;

        _followerSystem.StopAllFollowers(uid);

        component.Invisible = !component.Invisible;

        _visibilitySystem.SetLayer((uid, visibility),
            (ushort) (component.Invisible ? VisibilityFlags.AdminInvisible :
                EntityManager.HasComponent<GhostComponent>(uid) ? VisibilityFlags.Ghost : VisibilityFlags.Normal
            ));

        RaiseNetworkEvent(new InvisibilityToggleEvent(uid, component.Invisible));
    }
}
