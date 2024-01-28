using Content.Shared._White.Administration;
using Content.Shared.Eye;
using Content.Shared.Follower;
using Content.Shared.Ghost;
using Content.Shared.Follower.Components;
using Content.Shared._White.Administration;
using Robust.Server.GameObjects;
using InvisibilityComponent = Content.Shared._White.Administration.InvisibilityComponent;

namespace Content.Server._White.Administration;

public sealed class InvisibilitySystem : SharedInvisibilitySystem
{
    [Dependency] private readonly VisibilitySystem _visibilitySystem = default!;
    [Dependency] private readonly FollowerSystem _followerSystem = default!;
    [Dependency] private readonly SharedEyeSystem _eyeSystem = default!;

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
            _eyeSystem.SetVisibilityMask(uid, eye.VisibilityMask | (int) VisibilityFlags.AdminInvisible, eye);
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
            _eyeSystem.SetVisibilityMask(uid, eye.VisibilityMask & ~(int) VisibilityFlags.AdminInvisible, eye);
        }
    }

    public void ToggleInvisibility(EntityUid uid, InvisibilityComponent component)
    {
        if (!EntityManager.TryGetComponent(uid, out VisibilityComponent? visibility))
            return;

        if (TryComp(uid, out FollowedComponent? followed))
            _followerSystem.StopAllFollowers(uid, followed);

        component.Invisible = !component.Invisible;

        _visibilitySystem.SetLayer((uid, visibility),
            (ushort) (component.Invisible ? VisibilityFlags.AdminInvisible :
                EntityManager.HasComponent<GhostComponent>(uid) ? VisibilityFlags.Ghost : VisibilityFlags.Normal
            ));

        RaiseNetworkEvent(new InvisibilityToggleEvent(GetNetEntity(uid), component.Invisible));
    }
}
