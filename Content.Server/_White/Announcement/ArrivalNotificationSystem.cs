using System.Globalization;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.Radio.EntitySystems;
using Content.Shared._White.Announcement;
using Content.Shared.Radio;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server._White.Announcement;

public sealed class ArrivalNotificationSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly RadioSystem _radioSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeAllEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawn);
    }

    private void OnPlayerSpawn(PlayerSpawnCompleteEvent args)
    {
        if (args.JobId == null)
            return;

        if (!_prototypeManager.TryIndex<JobPrototype>(args.JobId, out var jobPrototype))
            return;

        if (jobPrototype.ArrivalNotificationPrototype == null)
            return;

        if (!_prototypeManager.TryIndex<ArrivalNotificationPrototype>(jobPrototype.ArrivalNotificationPrototype, out var notification))
            return;

        var message = GetMessage(args.Mob,
            jobPrototype,
            notification.UseGlobalAnnouncement ? notification.GlobalMessage : notification.Message);

        var senderName = Loc.GetString("head-arrived-sender");
        var source = args.Station;

        if (notification.UseGlobalAnnouncement)
            _chatSystem.DispatchGlobalAnnouncement(message, senderName, colorOverride: Color.Gold);

        message = GetMessage(args.Mob, jobPrototype, notification.Message); // Changing message type for radio notification

        foreach (var channel in notification.RadioChannelsPrototypes)
        {
            if (!_prototypeManager.TryIndex<RadioChannelPrototype>(channel, out _))
                continue;

            _radioSystem.SendRadioMessage(source, message, channel, args.Mob);
        }

    }

    private string GetMessage(EntityUid mob, JobPrototype jobPrototype, string type)
    {
        var message = Loc.GetString(type,
            ("character", MetaData(mob).EntityName),
            ("job", CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Loc.GetString(jobPrototype.Name))));

        return message;
    }
}
