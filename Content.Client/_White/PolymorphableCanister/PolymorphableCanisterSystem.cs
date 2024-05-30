using Content.Shared._White.PolymorphableCanister;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client._White.PolymorphableCanister;

public sealed class PolymorphableCanisterSystem : SharedPolymorphableCanisterSystem
{
    [Dependency] private readonly IComponentFactory _componentFactory = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PolymorphableCanisterComponent, AfterAutoHandleStateEvent>(HandleState);
    }

    private void HandleState(EntityUid uid,
        PolymorphableCanisterComponent component,
        ref AfterAutoHandleStateEvent args)
    {
        UpdateAppearance(uid, component.CurrentPrototype);
    }

    protected override void UpdateSprite(EntityUid uid, EntityPrototype proto)
    {
        base.UpdateSprite(uid, proto);

        if (!TryComp(uid, out SpriteComponent? sprite) ||
            !proto.TryGetComponent(out SpriteComponent? otherSprite, _componentFactory))
        {
            return;
        }

        sprite.CopyFrom(otherSprite);
    }
}
