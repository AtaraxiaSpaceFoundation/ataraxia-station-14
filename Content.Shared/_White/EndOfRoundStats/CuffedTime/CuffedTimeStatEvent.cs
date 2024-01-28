namespace Content.Shared._White.EndOfRoundStats.CuffedTime;

public sealed class CuffedTimeStatEvent : EntityEventArgs
{
    public TimeSpan Duration;

    public CuffedTimeStatEvent(TimeSpan duration)
    {
        Duration = duration;
    }
}
