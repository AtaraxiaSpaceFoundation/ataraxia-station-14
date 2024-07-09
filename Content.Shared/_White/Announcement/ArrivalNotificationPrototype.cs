using Robust.Shared.Prototypes;

namespace Content.Shared._White.Announcement;

[Prototype("arrivalNotification")]
public sealed partial class ArrivalNotificationPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;

    /// <summary>
    ///     The message that the department will receive upon the player arrival
    /// </summary>
    [DataField(required: true)]
    public string Message = default!;

    /// <summary>
    ///     The message that the station will receive upon the player arrival
    /// </summary>
    [DataField]
    public string GlobalMessage = default!;

    /// <summary>
    ///     ID of the channel where the player arrival will be announced.
    /// </summary>
    [DataField(required: true)]
    public HashSet<string> RadioChannelsPrototypes = default!;

    /// <summary>
    ///     Determines whether the notification will be made to the entire station. If false, the notification will be on the department radio channel
    /// </summary>
    [DataField]
    public bool UseGlobalAnnouncement;
}
