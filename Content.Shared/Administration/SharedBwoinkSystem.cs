using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration;

public abstract class SharedBwoinkSystem : EntitySystem
{
    // System users
    public static NetUserId SystemUserId { get; } = new(Guid.Empty);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<BwoinkTextMessage>(OnBwoinkTextMessage);
    }

    protected virtual void OnBwoinkTextMessage(BwoinkTextMessage message, EntitySessionEventArgs eventArgs)
    {
        // Specific side code in target.
    }

    protected void LogBwoink(BwoinkTextMessage message)
    {
    }

    [Serializable, NetSerializable]
    public sealed class BwoinkTextMessage(
        NetUserId userId,
        NetUserId trueSender,
        string text,
        bool isAdmin,
        DateTime? sentAt = default,
        bool playSound = true)
        : EntityEventArgs
    {
        public DateTime SentAt { get; } = sentAt ?? DateTime.Now;

        public NetUserId UserId { get; } = userId;

        // This is ignored from the client.
        // It's checked by the client when receiving a message from the server for bwoink noises.
        // This could be a boolean "Incoming", but that would require making a second instance.
        public NetUserId TrueSender { get; } = trueSender;

        public string Text { get; } = text;

        public bool IsAdmin { get; } = isAdmin;

        public bool PlaySound { get; } = playSound;
    }
}

/// <summary>
///     Sent by the server to notify all clients when the webhook url is sent.
///     The webhook url itself is not and should not be sent.
/// </summary>
[Serializable, NetSerializable]
public sealed class BwoinkDiscordRelayUpdated(bool enabled) : EntityEventArgs
{
    public bool DiscordRelayEnabled { get; } = enabled;
}

/// <summary>
///     Sent by the client to notify the server when it begins or stops typing.
/// </summary>
[Serializable, NetSerializable]
public sealed class BwoinkClientTypingUpdated(NetUserId channel, bool typing) : EntityEventArgs
{
    public NetUserId Channel { get; } = channel;

    public bool Typing { get; } = typing;
}

/// <summary>
///     Sent by server to notify admins when a player begins or stops typing.
/// </summary>
[Serializable, NetSerializable]
public sealed class BwoinkPlayerTypingUpdated(NetUserId channel, string playerName, bool typing) : EntityEventArgs
{
    public NetUserId Channel { get; } = channel;

    public string PlayerName { get; } = playerName;

    public bool Typing { get; } = typing;
}