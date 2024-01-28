using Content.Shared.Actions;
using Content.Shared.Examine;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Administration;

public abstract class SharedInvisibilitySystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InvisibilityComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<InvisibilityComponent, MapInitEvent>(OnInvisibilityInit);
        SubscribeLocalEvent<InvisibilityComponent, ComponentRemove>(OnInvisibilityRemove);
    }

    private void OnInvisibilityInit(EntityUid uid, InvisibilityComponent component, MapInitEvent args)
    {
        _actions.AddAction(uid, ref component.ToggleInvisibilityActionEntity, component.ToggleInvisibilityAction);
    }

    private void OnInvisibilityRemove(EntityUid uid, InvisibilityComponent component, ComponentRemove args)
    {
        _actions.RemoveAction(uid, component.ToggleInvisibilityActionEntity);
    }

    private void OnExamined(EntityUid uid, InvisibilityComponent component, ExaminedEvent args)
    {
        if (component.Invisible)
            args.PushMarkup("[color=lightsteelblue]Оно доступно лишь взору богов.[/color]");
    }
}

[Serializable, NetSerializable]
public sealed class InvisibilityToggleEvent : EntityEventArgs
{
    public NetEntity Uid { get; }
    public bool Invisible { get; }

    public InvisibilityToggleEvent(NetEntity uid, bool invisible)
    {
        Uid = uid;
        Invisible = invisible;
    }
}
