namespace Content.Shared._White.Cult.Systems;

/// <summary>
/// Thats need for chat perms update
/// </summary>
public sealed class CultistSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Components.CultistComponent, ComponentStartup>(OnInit);
        SubscribeLocalEvent<Components.CultistComponent, ComponentShutdown>(OnRemove);
    }

    private void OnInit(EntityUid uid, Components.CultistComponent component, ComponentStartup args)
    {
        RaiseLocalEvent(new EventCultistComponentState(true));
    }

    private void OnRemove(EntityUid uid, Components.CultistComponent component, ComponentShutdown args)
    {
        RaiseLocalEvent(new EventCultistComponentState(false));
    }
}

public sealed class EventCultistComponentState
{
    public bool Created { get; }
    public EventCultistComponentState(bool state)
    {
        Created = state;
    }
}
