namespace Content.Shared._White.Lighting;

public sealed class DoorlightsChangedEvent : EntityEventArgs
{
    public Enum? State;
    public bool Value;

    public DoorlightsChangedEvent(Enum? key, bool value)
    {
        State = key;
        Value = value;
    }
}
