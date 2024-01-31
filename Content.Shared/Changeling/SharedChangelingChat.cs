namespace Content.Shared.Changeling;

public sealed class SharedChangelingChat : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingComponent, ComponentStartup>(OnInit);
        SubscribeLocalEvent<ChangelingComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnInit(EntityUid uid, ChangelingComponent component, ComponentStartup args)
    {
        RaiseLocalEvent(new ChangelingUserStart(true));
    }

    private void OnShutdown(EntityUid uid, ChangelingComponent component, ComponentShutdown args)
    {
        RaiseLocalEvent(new ChangelingUserStart(false));
    }
}


public sealed class ChangelingUserStart
{
    public bool Created { get; }

    public ChangelingUserStart(bool state)
    {
        Created = state;
    }
}
