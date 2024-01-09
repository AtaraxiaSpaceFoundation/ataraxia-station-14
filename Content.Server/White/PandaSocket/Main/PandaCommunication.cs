using System.Text.Json.Serialization;

namespace Content.Server.White.PandaSocket.Main;

public class PandaBaseMessage
{
    [JsonPropertyName("command")]
    public virtual string? Command { get; set; }
}

public class UtkaOOCRequest : PandaBaseMessage
{
    [JsonPropertyName("command")]
    public override string? Command => "ooc";

    [JsonPropertyName("ckey")]
    public string? CKey { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

public class UtkaAsayRequest : PandaBaseMessage
{
    [JsonPropertyName("command")]
    public override string? Command => "asay";

    [JsonPropertyName("ackey")]
    public string? ACkey { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

public class UtkaPmRequest : PandaBaseMessage
{
    [JsonPropertyName("command")]
    public override string? Command => "discordpm";

    [JsonPropertyName("sender")]
    public string? Sender { get; set; }

    [JsonPropertyName("receiver")]
    public string? Receiver { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

public class UtkaPmResponse : PandaBaseMessage
{
    [JsonPropertyName("command")]
    public override string? Command => "discordpm";

    [JsonPropertyName("message")]
    public bool? Message { get; set; }
}

public class UtkaWhoRequest : PandaBaseMessage
{
    [JsonPropertyName("command")]
    public override string? Command => "who";
}

public class UtkaWhoResponse : PandaBaseMessage
{
    [JsonPropertyName("command")]
    public override string? Command => "who";

    [JsonPropertyName("players")]
    public List<string>? Players { get; set; }
}

public class UtkaAdminWhoRequest : PandaBaseMessage
{
    [JsonPropertyName("command")]
    public override string? Command => "adminwho";
}

public class UtkaAdminWhoResponse : PandaBaseMessage
{
    [JsonPropertyName("command")]
    public override string? Command => "adminwho";

    [JsonPropertyName("admins")]
    public List<string>? Admins { get; set; }
}

public class UtkaStatusRequest : PandaBaseMessage
{
    [JsonPropertyName("command")]
    public override string? Command => "status";
}

public class UtkaStatusResponse : PandaBaseMessage
{
    [JsonPropertyName("command")]
    public override string? Command => "status";

    [JsonPropertyName("players")]
    public int? Players { get; set; }

    [JsonPropertyName("admins")]
    public int? Admins { get; set; }

    [JsonPropertyName("map")]
    public string? Map { get; set; }

    [JsonPropertyName("roundduration")]
    public double RoundDuration { get; set; }

    [JsonPropertyName("shuttlestatus")]
    public string? ShuttleStatus { get; set; }

    [JsonPropertyName("stationcode")]
    public string? StationCode { get; set; }
}

public sealed class UtkaBanRequest : PandaBaseMessage
{
    [JsonPropertyName("command")]
    public override string? Command => "ban";

    [JsonPropertyName("ckey")]
    public string? Ckey { get; set; }

    [JsonPropertyName("ackey")]
    public string? ACkey { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("duration")]
    public uint? Duration { get; set; }

    [JsonPropertyName("global")]
    public bool? Global { get; set; }
}

public sealed class UtkaBanResponse : PandaBaseMessage
{
    [JsonPropertyName("command")]
    public override string? Command => "ban";

    [JsonPropertyName("banned")]
    public bool? Banned { get; set; }
}

public sealed class UtkaJobBanRequest : PandaBaseMessage
{
    public override string? Command => "jobban";

    [JsonPropertyName("ckey")]
    public string? Ckey { get; set; }

    [JsonPropertyName("ackey")]
    public string? ACkey { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("duration")]
    public uint? Duration { get; set; }

    [JsonPropertyName("global")]
    public bool? Global { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }
}

public sealed class UtkaJobBanResponse : PandaBaseMessage
{
    [JsonPropertyName("command")]
    public override string? Command => "jobban";

    [JsonPropertyName("banned")]
    public bool? Banned { get; set; }
}

public sealed class UtkaRestartRoundRequest : PandaBaseMessage
{
    [JsonPropertyName("command")]
    public override string? Command => "restartround";
}

public sealed class UtkaRestartRoundResponse : PandaBaseMessage
{
    [JsonPropertyName("command")]
    public override string? Command => "restartround";

    [JsonPropertyName("restarted")]
    public bool? Restarted { get; set; }
}

public sealed class UtkaUnbanRequest : PandaBaseMessage
{
    [JsonPropertyName("command")]
    public override string? Command => "unban";

    [JsonPropertyName("ackey")]
    public string? ACkey { get; set; }

    [JsonPropertyName("bid")]
    public int? Bid { get; set; }
}

public sealed class UtkaUnbanResponse : PandaBaseMessage
{
    [JsonPropertyName("command")]
    public override string? Command => "unban";

    [JsonPropertyName("unbanned")]
    public bool? Unbanned { get; set; }
}

public sealed class UtkaUnJobBanRequest : PandaBaseMessage
{
    [JsonPropertyName("command")]
    public override string? Command => "unjobban";

    [JsonPropertyName("ackey")]
    public string? ACkey { get; set; }

    [JsonPropertyName("bid")]
    public int? Bid { get; set; }
}

public sealed class UtkaUnJobBanResponse : PandaBaseMessage
{
    [JsonPropertyName("command")]
    public override string? Command => "unjobban";

    [JsonPropertyName("unbanned")]
    public bool? Unbanned { get; set; }
}

public sealed class UtkaBannedEvent : PandaBaseMessage
{
    [JsonPropertyName("command")]
    public override string? Command => "banned";

    [JsonPropertyName("ckey")]
    public string? Ckey { get; set; }

    [JsonPropertyName("ackey")]
    public string? ACkey { get; set; }

    [JsonPropertyName("bantype")]
    public string? Bantype { get; set; }

    [JsonPropertyName("duration")]
    public uint? Duration { get; set; }

    [JsonPropertyName("global")]
    public bool? Global { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("rid")]
    public int? Rid { get; set; }

    [JsonPropertyName("banid")]
    public int? BanId { get; set; }
}

public sealed class UtkaChatEventMessage : PandaBaseMessage
{
    [JsonPropertyName("ckey")]
    public string? Ckey { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

public sealed class UtkaRoundStatusEvent : PandaBaseMessage
{
    [JsonPropertyName("command")]
    public override string? Command => "roundstatus";

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

public sealed class UtkaChatMeEvent : PandaBaseMessage
{
    [JsonPropertyName("command")]
    public override string? Command => "me";

    [JsonPropertyName("ckey")]
    public string? Ckey { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("character_name")]
    public string? CharacterName { get; set; }
}

public sealed class UtkaAhelpPmEvent : PandaBaseMessage
{
    [JsonPropertyName("command")]
    public override string? Command => "pm";

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("ckey")]
    public string? Ckey { get; set; }

    [JsonPropertyName("sender")]
    public string? Sender { get; set; }

    [JsonPropertyName("rid")]
    public int? Rid { get; set; }

    [JsonPropertyName("no_admins")]
    public bool? NoAdmins { get; set; }

    [JsonPropertyName("entity")]
    public string? Entity { get; set; }
}
