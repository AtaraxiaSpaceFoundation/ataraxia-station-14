using System.Linq;
using System.Text;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.Speech.Components;
using Content.Server.Speech.EntitySystems;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Server._White.AspectsSystem.Aspects.Components;
using Content.Server._White.Other.Speech;
using Content.Server._White.PandaSocket.Main;
using Content.Shared.ActionBlocker;
using Content.Shared.Aliens.Components;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Changeling;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Ghost;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Systems;
using Content.Shared.Players;
using Content.Shared.Radio;
using Content.Shared._White;
using Content.Shared.Speech;
using Content.Shared._White.Cult.Systems;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Replays;
using Robust.Shared.Utility;
using CultistComponent = Content.Shared._White.Cult.Components.CultistComponent;

namespace Content.Server.Chat.Systems;

// TODO refactor whatever active warzone this class and chatmanager have become
/// <summary>
///     ChatSystem is responsible for in-simulation chat handling, such as whispering, speaking, emoting, etc.
///     ChatSystem depends on ChatManager to actually send the messages.
/// </summary>
public sealed partial class ChatSystem : SharedChatSystem
{
    [Dependency] private readonly IReplayRecordingManager _replay = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IChatSanitizationManager _sanitizer = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly ReplacementAccentSystem _wordreplacement = default!;
    [Dependency] private readonly INetConfigurationManager _netConfigurationManager = default!; // WD
    [Dependency] private readonly GameTicker _gameTicker = default!;                            // WD
    [Dependency] private readonly CultistWordGeneratorManager _wordGenerator = default!;        // WD

    //WD-EDIT
    [Dependency] private readonly PandaWebManager _pandaWeb = default!;
    //WD-EDIT

    public const int VoiceRange = 10;         // how far voice goes in world units
    public const int WhisperClearRange = 2;   // how far whisper goes while still being understandable, in world units
    public const int WhisperMuffledRange = 5; // how far whisper goes at all, in world units
    public const string DefaultAnnouncementSound = "/Audio/Announcements/announce.ogg";

    private bool _loocEnabled = true;
    private bool _deadLoocEnabled;
    private bool _critLoocEnabled;
    private readonly bool _adminLoocEnabled = true;

    public override void Initialize()
    {
        base.Initialize();
        CacheEmotes();
        Subs.CVar(_configurationManager, CCVars.LoocEnabled, OnLoocEnabledChanged, true);
        Subs.CVar(_configurationManager, CCVars.DeadLoocEnabled, OnDeadLoocEnabledChanged, true);
        Subs.CVar(_configurationManager, CCVars.CritLoocEnabled, OnCritLoocEnabledChanged, true);

        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnGameChange);
    }

    private void OnLoocEnabledChanged(bool val)
    {
        if (_loocEnabled == val)
            return;

        _loocEnabled = val;
        _chatManager.DispatchServerAnnouncement(
            Loc.GetString(val ? "chat-manager-looc-chat-enabled-message" : "chat-manager-looc-chat-disabled-message"));
    }

    private void OnDeadLoocEnabledChanged(bool val)
    {
        if (_deadLoocEnabled == val)
            return;

        _deadLoocEnabled = val;
        _chatManager.DispatchServerAnnouncement(
            Loc.GetString(val
                ? "chat-manager-dead-looc-chat-enabled-message"
                : "chat-manager-dead-looc-chat-disabled-message"));
    }

    private void OnCritLoocEnabledChanged(bool val)
    {
        if (_critLoocEnabled == val)
            return;

        _critLoocEnabled = val;
        _chatManager.DispatchServerAnnouncement(
            Loc.GetString(val
                ? "chat-manager-crit-looc-chat-enabled-message"
                : "chat-manager-crit-looc-chat-disabled-message"));
    }

    private void OnGameChange(GameRunLevelChangedEvent ev)
    {
        switch (ev.New)
        {
            case GameRunLevel.InRound:
                if (!_configurationManager.GetCVar(CCVars.OocEnableDuringRound))
                    _configurationManager.SetCVar(CCVars.OocEnabled, false);

                break;
            case GameRunLevel.PostRound:
                if (!_configurationManager.GetCVar(CCVars.OocEnableDuringRound))
                    _configurationManager.SetCVar(CCVars.OocEnabled, true);

                break;
        }
    }

    /// <summary>
    ///     Sends an in-character chat message to relevant clients.
    /// </summary>
    /// <param name="source">The entity that is speaking</param>
    /// <param name="message">The message being spoken or emoted</param>
    /// <param name="desiredType">The chat type</param>
    /// <param name="hideChat">Whether or not this message should appear in the chat window</param>
    /// <param name="hideLog">Whether or not this message should appear in the adminlog window</param>
    /// <param name="shell"></param>
    /// <param name="player">The player doing the speaking</param>
    /// <param name="nameOverride">The name to use for the speaking entity. Usually this should just be modified via <see cref="TransformSpeakerNameEvent"/>. If this is set, the event will not get raised.</param>
    public void TrySendInGameICMessage(
        EntityUid source,
        string message,
        InGameICChatType desiredType,
        bool hideChat,
        bool hideLog = false,
        IConsoleShell? shell = null,
        ICommonSession? player = null,
        string? nameOverride = null,
        bool checkRadioPrefix = true,
        bool ignoreActionBlocker = false)
    {
        TrySendInGameICMessage(source, message, desiredType,
            hideChat ? ChatTransmitRange.HideChat : ChatTransmitRange.Normal, hideLog, shell, player, nameOverride,
            checkRadioPrefix, ignoreActionBlocker);
    }

    /// <summary>
    ///     Sends an in-character chat message to relevant clients.
    /// </summary>
    /// <param name="source">The entity that is speaking</param>
    /// <param name="message">The message being spoken or emoted</param>
    /// <param name="desiredType">The chat type</param>
    /// <param name="range">Conceptual range of transmission, if it shows in the chat window, if it shows to far-away ghosts or ghosts at all...</param>
    /// <param name="shell"></param>
    /// <param name="player">The player doing the speaking</param>
    /// <param name="nameOverride">The name to use for the speaking entity. Usually this should just be modified via <see cref="TransformSpeakerNameEvent"/>. If this is set, the event will not get raised.</param>
    /// <param name="ignoreActionBlocker">If set to true, action blocker will not be considered for whether an entity can send this message.</param>
    public void TrySendInGameICMessage(
        EntityUid source,
        string message,
        InGameICChatType desiredType,
        ChatTransmitRange range,
        bool hideLog = false,
        IConsoleShell? shell = null,
        ICommonSession? player = null,
        string? nameOverride = null,
        bool checkRadioPrefix = true,
        bool ignoreActionBlocker = false
    )
    {
        if (HasComp<GhostComponent>(source))
        {
            if (desiredType == InGameICChatType.Emote)
                return;

            // Ghosts can only send dead chat messages, so we'll forward it to InGame OOC.
            TrySendInGameOOCMessage(source, message, InGameOOCChatType.Dead, range == ChatTransmitRange.HideChat, shell,
                player);

            return;
        }

        if (player != null && !_chatManager.HandleRateLimit(player))
            return;

        // Sus
        if (player?.AttachedEntity is { Valid: true } entity && source != entity)
        {
            return;
        }

        if (!CanSendInGame(message, shell, player))
            return;

        ignoreActionBlocker = CheckIgnoreSpeechBlocker(source, ignoreActionBlocker);

        // this method is a disaster
        // every second i have to spend working with this code is fucking agony
        // scientists have to wonder how any of this was merged
        // coding any game admin feature that involves chat code is pure torture
        // changing even 10 lines of code feels like waterboarding myself
        // and i dont feel like vibe checking 50 code paths
        // so we set this here
        // todo free me from chat code
        if (player != null)
        {
            _chatManager.EnsurePlayer(player.UserId).AddEntity(GetNetEntity(source));
        }

        if (desiredType == InGameICChatType.Speak && message.StartsWith(LocalPrefix))
        {
            // prevent radios and remove prefix.
            checkRadioPrefix = false;
            message = message[1..];
        }

        var shouldCapitalize = (desiredType != InGameICChatType.Emote);
        var shouldPunctuate = _configurationManager.GetCVar(CCVars.ChatPunctuation);
        var sanitizeSlang = _configurationManager.GetCVar(WhiteCVars.ChatSlangFilter);

        message = SanitizeInGameICMessage(source, message, out var emoteStr, shouldCapitalize, shouldPunctuate,
            sanitizeSlang);

        // Was there an emote in the message? If so, send it.
        if (emoteStr != message && emoteStr != null)
        {
            SendEntityEmote(source, emoteStr, range, nameOverride, ignoreActionBlocker);
        }

        // This can happen if the entire string is sanitized out.
        if (string.IsNullOrEmpty(message))
            return;

        if (desiredType != InGameICChatType.Emote && player is not null &&
            !_chatManager.TrySendNewMessage(player, message, true)) // WD
            return;

        // This message may have a radio prefix, and should then be whispered to the resolved radio channel
        if (checkRadioPrefix)
        {
            if (TryProccessRadioMessage(source, message, out var modMessage, out var channel))
            {
                SendEntityWhisper(source, modMessage, range, channel, nameOverride, hideLog, ignoreActionBlocker);
                return;
            }
        }

        if (desiredType == InGameICChatType.Speak &&
            _gameTicker.GetActiveGameRules().Where(HasComp<WhisperAspectComponent>).Any()) // WD
        {
            desiredType = InGameICChatType.Whisper;
        }

        // Otherwise, send whatever type.
        switch (desiredType)
        {
            case InGameICChatType.Speak:
                SendEntitySpeak(source, message, range, nameOverride, hideLog, ignoreActionBlocker);
                break;
            case InGameICChatType.Whisper:
                SendEntityWhisper(source, message, range, null, nameOverride, hideLog, ignoreActionBlocker);
                break;
            case InGameICChatType.Emote:
                SendEntityEmote(source, message, range, nameOverride, hideLog: hideLog,
                    ignoreActionBlocker: ignoreActionBlocker);

                break;
        }
    }

    public void TrySendInGameOOCMessage(
        EntityUid source,
        string message,
        InGameOOCChatType type,
        bool hideChat,
        IConsoleShell? shell = null,
        ICommonSession? player = null
    )
    {
        if (!CanSendInGame(message, shell, player))
            return;

        if (player != null && !_chatManager.HandleRateLimit(player))
            return;

        // It doesn't make any sense for a non-player to send in-game OOC messages, whereas non-players may be sending
        // in-game IC messages.
        if (player?.AttachedEntity is not { Valid: true } entity || source != entity)
            return;

        message = SanitizeInGameOOCMessage(message);

        var sendType = type;
        // If dead player LOOC is disabled, unless you are an aghost, send dead messages to dead chat
        if (!_adminManager.IsAdmin(player) && !_deadLoocEnabled &&
            (HasComp<GhostComponent>(source) || _mobStateSystem.IsDead(source)))
            sendType = InGameOOCChatType.Dead;

        // If crit player LOOC is disabled, don't send the message at all.
        if (!_critLoocEnabled && _mobStateSystem.IsCritical(source))
            return;

        if (!_chatManager.TrySendNewMessage(player, message)) // WD
            return;

        switch (sendType)
        {
            case InGameOOCChatType.Dead:
                SendDeadChat(source, player, message, hideChat);
                break;
            case InGameOOCChatType.Looc:
                SendLOOC(source, player, message, hideChat);
                break;
            case InGameOOCChatType.Changeling:
                SendChangelingChat(source, player, message, hideChat);
                break;
            case InGameOOCChatType.Cult:
                SendCultChat(source, player, message, hideChat);
                break;
        }
    }

    public string AfterSpeechTransformed(EntityUid sender, string message)
    {
        var ev = new SpeechTransformedEvent(sender, message);
        RaiseLocalEvent(ev);
        return ev.Message;
    }

    public void TrySendXenoHivemindMessage(
        EntityUid source,
        string message,
        InGameOOCChatType type,
        bool hideChat,
        IConsoleShell? shell = null,
        ICommonSession? player = null
    )
    {
        if (!CanSendInGame(message, shell, player))
            return;

        if (player != null && !_chatManager.HandleRateLimit(player))
            return;

        // It doesn't make any sense for a non-player to send in-game OOC messages, whereas non-players may be sending
        // in-game IC messages.
        if (player?.AttachedEntity is not { Valid: true } entity || source != entity)
            return;

        message = SanitizeInGameOOCMessage(message);

        var sendType = type;

        switch (sendType)
        {
            case InGameOOCChatType.HiveXeno:
                SendXenoHivemindChat(source, player, message, hideChat);
                break;
        }
    }

#region Announcements

    /// <summary>
    /// Dispatches an announcement to all.
    /// </summary>
    /// <param name="message">The contents of the message</param>
    /// <param name="sender">The sender (Communications Console in Communications Console Announcement)</param>
    /// <param name="playSound">Play the announcement sound</param>
    /// <param name="colorOverride">Optional color for the announcement message</param>
    public void DispatchGlobalAnnouncement(
        string message,
        string sender = "Центральное командование",
        bool playSound = true,
        SoundSpecifier? announcementSound = null,
        Color? colorOverride = null
    )
    {
        var wrappedMessage = Loc.GetString("chat-manager-sender-announcement-wrap-message", ("sender", sender),
            ("message", FormattedMessage.EscapeText(message)));

        _chatManager.ChatMessageToAll(ChatChannel.Radio, message, wrappedMessage, default, false, true, colorOverride);
        if (playSound)
        {
            _audio.PlayGlobal(
                announcementSound is not null ? _audio.GetSound(announcementSound) : DefaultAnnouncementSound,
                Filter.Broadcast(),
                true,
                AudioParams.Default.WithVolume(-2f));
        }

        _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Global station announcement from {sender}: {message}");
    }

    /// <summary>
    /// Dispatches an announcement on a specific station
    /// </summary>
    /// <param name="source">The entity making the announcement (used to determine the station)</param>
    /// <param name="message">The contents of the message</param>
    /// <param name="sender">The sender (Communications Console in Communications Console Announcement)</param>
    /// <param name="playDefaultSound">Play the announcement sound</param>
    /// <param name="colorOverride">Optional color for the announcement message</param>
    public void DispatchStationAnnouncement(
        EntityUid source,
        string message,
        string sender = "Центральное командование",
        bool playDefaultSound = true,
        SoundSpecifier? announcementSound = null,
        Color? colorOverride = null)
    {
        var wrappedMessage = Loc.GetString("chat-manager-sender-announcement-wrap-message", ("sender", sender),
            ("message", FormattedMessage.EscapeText(message)));

        var station = _stationSystem.GetOwningStation(source);

        if (station == null)
        {
            // you can't make a station announcement without a station
            return;
        }

        if (!EntityManager.TryGetComponent<StationDataComponent>(station, out var stationDataComp))
            return;

        var filter = _stationSystem.GetInStation(stationDataComp);

        _chatManager.ChatMessageToManyFiltered(filter, ChatChannel.Radio, message, wrappedMessage, source, false, true,
            colorOverride);

        if (playDefaultSound)
        {
            _audio.PlayGlobal(
                announcementSound is not null ? _audio.GetSound(announcementSound) : DefaultAnnouncementSound,
                filter,
                true,
                AudioParams.Default.WithVolume(-2f));
        }

        _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Station Announcement on {station} from {sender}: {message}");
    }

#endregion

#region Private API

    private void SendEntitySpeak(
        EntityUid source,
        string originalMessage,
        ChatTransmitRange range,
        string? nameOverride,
        bool hideLog = false,
        bool ignoreActionBlocker = false
    )
    {
        if (!_actionBlocker.CanSpeak(source) && !ignoreActionBlocker)
            return;

        var message = TransformSpeech(source, FormattedMessage.RemoveMarkup(originalMessage));

        if (message.Length == 0)
            return;

        message = AfterSpeechTransformed(source, message);
        var speech = GetSpeechVerb(source, message);

        // get the entity's apparent name (if no override provided).
        string name;
        if (nameOverride != null)
        {
            name = nameOverride;
        }
        else
        {
            var nameEv = new TransformSpeakerNameEvent(source, Name(source));
            RaiseLocalEvent(source, nameEv);
            name = nameEv.Name;
            // Check for a speech verb override
            if (nameEv.SpeechVerb != null &&
                _prototypeManager.TryIndex<SpeechVerbPrototype>(nameEv.SpeechVerb, out var proto))
                speech = proto;
        }

        name = FormattedMessage.EscapeText(name);

        var wrappedMessage = Loc.GetString(speech.Bold ? "chat-manager-entity-say-bold-wrap-message" : "chat-manager-entity-say-wrap-message",
            ("entityName", name),
            ("verb", Loc.GetString(_random.Pick(speech.SpeechVerbStrings))),
            ("fontType", speech.FontId),
            ("fontSize", speech.FontSize),
            ("message", message));

        //WD-EDIT
        if (TryComp<VoiceOfGodComponent>(source, out var comp))
        {
            wrappedMessage = Loc.GetString(comp.ChatLoc,
                ("entityName", name),
                ("message", message),
                ("color", comp.ChatColor)
            );
        }
        //WD-EDIT

        SendInVoiceRange(ChatChannel.Local, message, wrappedMessage, source, range);

        var ev = new EntitySpokeEvent(source, message, originalMessage, null, null);
        RaiseLocalEvent(source, ev, true);

        // To avoid logging any messages sent by entities that are not players, like vendors, cloning, etc.
        // Also doesn't log if hideLog is true.
        if (!HasComp<ActorComponent>(source) || hideLog)
            return;

        if (originalMessage == message)
        {
            if (name != Name(source))
            {
                _adminLogger.Add(LogType.Chat, LogImpact.Low,
                    $"Say from {ToPrettyString(source):user} as {name}: {originalMessage}.");
            }
            else
            {
                _adminLogger.Add(LogType.Chat, LogImpact.Low,
                    $"Say from {ToPrettyString(source):user}: {originalMessage}.");
            }
        }
        else
        {
            if (name != Name(source))
            {
                _adminLogger.Add(LogType.Chat, LogImpact.Low,
                    $"Say from {ToPrettyString(source):user} as {name}, original: {originalMessage}, transformed: {message}.");
            }
            else
            {
                _adminLogger.Add(LogType.Chat, LogImpact.Low,
                    $"Say from {ToPrettyString(source):user}, original: {originalMessage}, transformed: {message}.");
            }
        }
    }

    private void SendEntityWhisper(
        EntityUid source,
        string originalMessage,
        ChatTransmitRange range,
        RadioChannelPrototype? channel,
        string? nameOverride,
        bool hideLog = false,
        bool ignoreActionBlocker = false
    )
    {
        if (!_actionBlocker.CanSpeak(source) && !ignoreActionBlocker)
            return;

        var message = TransformSpeech(source, FormattedMessage.RemoveMarkup(originalMessage));
        if (message.Length == 0)
            return;

        message = AfterSpeechTransformed(source, message);

        var obfuscatedMessage = ObfuscateMessageReadability(message, 0.2f);

        // get the entity's name by visual identity (if no override provided).
        var nameIdentity = FormattedMessage.EscapeText(nameOverride ?? Identity.Name(source, EntityManager));
        // get the entity's name by voice (if no override provided).
        string name;
        if (nameOverride != null)
        {
            name = nameOverride;
        }
        else
        {
            var nameEv = new TransformSpeakerNameEvent(source, Name(source));
            RaiseLocalEvent(source, nameEv);
            name = nameEv.Name;
        }

        name = FormattedMessage.EscapeText(name);

        var wrappedMessage = Loc.GetString("chat-manager-entity-whisper-wrap-message",
            ("entityName", name), ("message", message));

        var wrappedobfuscatedMessage = Loc.GetString("chat-manager-entity-whisper-wrap-message",
            ("entityName", nameIdentity), ("message", obfuscatedMessage));

        var wrappedUnknownMessage = Loc.GetString("chat-manager-entity-whisper-unknown-wrap-message",
            ("message", obfuscatedMessage));

        foreach (var (session, data) in GetRecipients(source, WhisperMuffledRange))
        {
            if (session.AttachedEntity is not { Valid: true })
                continue;

            var listener = session.AttachedEntity.Value;

            if (MessageRangeCheck(session, data, range) != MessageRangeCheckResult.Full)
                continue; // Won't get logged to chat, and ghosts are too far away to see the pop-up, so we just won't send it to them.

            if (data.Range <= WhisperClearRange)
            {
                _chatManager.ChatMessageToOne(ChatChannel.Whisper, message, wrappedMessage, source, false,
                    session.Channel);
            }
            //If listener is too far, they only hear fragments of the message
            //Collisiongroup.Opaque is not ideal for this use. Preferably, there should be a check specifically with "Can Ent1 see Ent2" in mind
            else if (_interactionSystem.InRangeUnobstructed(source, listener, WhisperMuffledRange,
                         Shared.Physics.CollisionGroup.Opaque)) //Shared.Physics.CollisionGroup.Opaque
            {
                _chatManager.ChatMessageToOne(ChatChannel.Whisper, obfuscatedMessage, wrappedobfuscatedMessage, source,
                    false, session.Channel);
            }
            //If listener is too far and has no line of sight, they can't identify the whisperer's identity
            else
            {
                _chatManager.ChatMessageToOne(ChatChannel.Whisper, obfuscatedMessage, wrappedUnknownMessage, source,
                    false, session.Channel);
            }
        }

        _replay.RecordServerMessage(new ChatMessage(ChatChannel.Whisper, message, wrappedMessage, GetNetEntity(source),
            null, MessageRangeHideChatForReplay(range)));

        var ev = new EntitySpokeEvent(source, message, originalMessage, channel, obfuscatedMessage);
        RaiseLocalEvent(source, ev, true);
        if (hideLog)
            return;

        if (originalMessage == message)
        {
            if (name != Name(source))
            {
                _adminLogger.Add(LogType.Chat, LogImpact.Low,
                    $"Whisper from {ToPrettyString(source):user} as {name}: {originalMessage}.");
            }
            else
            {
                _adminLogger.Add(LogType.Chat, LogImpact.Low,
                    $"Whisper from {ToPrettyString(source):user}: {originalMessage}.");
            }
        }
        else
        {
            if (name != Name(source))
            {
                _adminLogger.Add(LogType.Chat, LogImpact.Low,
                    $"Whisper from {ToPrettyString(source):user} as {name}, original: {originalMessage}, transformed: {message}.");
            }
            else
            {
                _adminLogger.Add(LogType.Chat, LogImpact.Low,
                    $"Whisper from {ToPrettyString(source):user}, original: {originalMessage}, transformed: {message}.");
            }
        }
    }

    private void SendEntityEmote(
        EntityUid source,
        string action,
        ChatTransmitRange range,
        string? nameOverride,
        bool hideLog = false,
        bool checkEmote = true,
        bool ignoreActionBlocker = false,
        NetUserId? author = null
    )
    {
        if (!_actionBlocker.CanEmote(source) && !ignoreActionBlocker)
            return;

        // get the entity's apparent name (if no override provided).
        var ent = Identity.Entity(source, EntityManager);
        var name = FormattedMessage.EscapeText(nameOverride ?? Name(ent));

        // Emotes use Identity.Name, since it doesn't actually involve your voice at all.
        var wrappedMessage = Loc.GetString("chat-manager-entity-me-wrap-message",
            ("entityName", name),
            ("entity", ent),
            ("message", FormattedMessage.RemoveMarkup(action)));

        if (checkEmote)
            TryEmoteChatInput(source, action);

        SendInVoiceRange(ChatChannel.Emotes, action, wrappedMessage, source, range, author);
        if (hideLog)
            return;

        if (name != Name(source))
        {
            _adminLogger.Add(LogType.Chat, LogImpact.Low,
                $"Emote from {ToPrettyString(source):user} as {name}: {action}");
        }
        else
        {
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Emote from {ToPrettyString(source):user}: {action}");
        }

        //WD-EDIT

        var ckey = string.Empty;

        if (TryComp<ActorComponent>(source, out var actorComponent))
        {
            ckey = actorComponent.PlayerSession.Name;
        }

        if (string.IsNullOrEmpty(ckey))
            return;

        var utkaEmoteEvent = new UtkaChatMeEvent
        {
            Ckey = ckey,
            Message = action,
            CharacterName = MetaData(source).EntityName
        };

        _pandaWeb.SendBotPostMessage(utkaEmoteEvent);

        //WD-EDIT
    }

    // ReSharper disable once InconsistentNaming
    private void SendLOOC(EntityUid source, ICommonSession player, string message, bool hideChat)
    {
        var name = FormattedMessage.EscapeText(Identity.Name(source, EntityManager));

        if (_adminManager.HasAdminFlag(player, AdminFlags.Admin))
        {
            if (!_adminLoocEnabled)
                return;
        }
        else if (!_loocEnabled)
        {
            return;
        }

        // If crit player LOOC is disabled, don't send the message at all.
        if (!_critLoocEnabled && _mobStateSystem.IsCritical(source))
            return;

        var wrappedMessage = Loc.GetString("chat-manager-entity-looc-wrap-message",
            ("entityName", name),
            ("message", FormattedMessage.EscapeText(message)));

        SendInVoiceRange(ChatChannel.LOOC, message, wrappedMessage, source,
            hideChat ? ChatTransmitRange.HideChat : ChatTransmitRange.Normal, player.UserId);

        _adminLogger.Add(LogType.Chat, LogImpact.Low, $"LOOC from {player:Player}: {message}");
    }


    private void SendChangelingChat(EntityUid source, ICommonSession player, string message, bool hideChat)
    {
        if (!TryComp<ChangelingComponent>(source, out var changeling))
            return;

        var clients = GetChangelingChatClients();

        var playerName = changeling.HiveName;

        message = $"{char.ToUpper(message[0])}{message[1..]}";

        var wrappedMessage = Loc.GetString("chat-manager-send-changeling-chat-wrap-message",
            ("player", playerName),
            ("message", FormattedMessage.EscapeText(message)));

        _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Changeling chat from {player:Player}-({playerName}): {message}");

        _chatManager.ChatMessageToMany(ChatChannel.Changeling, message, wrappedMessage, source,
            hideChat, false, clients.ToList());
    }

    private IEnumerable<INetChannel> GetChangelingChatClients()
    {
        return Filter.Empty()
            .AddWhereAttachedEntity(HasComp<GhostComponent>)
            .AddWhereAttachedEntity(HasComp<ChangelingComponent>)
            .Recipients
            .Select(p => p.Channel);
    }

    // WD EDIT
    private void SendCultChat(EntityUid source, ICommonSession player, string message, bool hideChat)
    {
        var clients = GetCultChatClients();
        var playerName = Name(source);
        var wrappedMessage = Loc.GetString("chat-manager-send-cult-chat-wrap-message",
            ("channelName", Loc.GetString("chat-manager-cult-channel-name")),
            ("player", playerName),
            ("message", FormattedMessage.EscapeText(message)));

        _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Cult chat from {player:Player}: {message}");

        _chatManager.ChatMessageToMany(ChatChannel.Cult, message, wrappedMessage, source, hideChat, false,
            clients.ToList());

        SendEntityWhisper(source, _wordGenerator.GenerateText(message), ChatTransmitRange.Normal, null, null);
    }

    private IEnumerable<INetChannel> GetCultChatClients()
    {
        return Filter.Empty()
            .AddWhereAttachedEntity(HasComp<GhostComponent>)
            .AddWhereAttachedEntity(HasComp<CultistComponent>)
            .Recipients
            .Union(_adminManager.ActiveAdmins)
            .Select(p => p.Channel);
    }

    private void SendDeadChat(EntityUid source, ICommonSession player, string message, bool hideChat)
    {
        var clients = GetDeadChatClients();
        var playerName = Name(source);
        string wrappedMessage;
        if (_adminManager.IsAdmin(player) &&
            _netConfigurationManager.GetClientCVar(player.Channel, WhiteCVars.DeadChatAdmin)) // WD
        {
            wrappedMessage = Loc.GetString("chat-manager-send-admin-dead-chat-wrap-message",
                ("adminChannelName", Loc.GetString("chat-manager-admin-channel-name")),
                ("userName", player.Channel.UserName),
                ("message", FormattedMessage.EscapeText(message)));

            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Admin dead chat from {player:Player}: {message}");
        }
        else
        {
            wrappedMessage = Loc.GetString("chat-manager-send-dead-chat-wrap-message",
                ("deadChannelName", Loc.GetString("chat-manager-dead-channel-name")),
                ("playerName", (playerName)),
                ("message", FormattedMessage.EscapeText(message)));

            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Dead chat from {player:Player}: {message}");
        }

        _chatManager.ChatMessageToMany(ChatChannel.Dead, message, wrappedMessage, source, hideChat, true,
            clients.ToList(), author: player.UserId);
    }

    private void SendXenoHivemindChat(EntityUid source, ICommonSession player, string message, bool hideChat)
    {
        var clients = GetXenoChatClients();
        var playerName = Name(source);
        var speech = GetSpeechVerb(source, message);
        string wrappedMessage;
        if (!HasComp<AlienQueenComponent>(source))
        {
            wrappedMessage = Loc.GetString(speech.Bold ? "chat-manager-entity-say-bold-wrap-message" : "chat-manager-entity-say-wrap-message",
                ("entityName", playerName),
                ("verb", Loc.GetString(_random.Pick(speech.SpeechVerbStrings))),
                ("fontType", speech.FontId),
                ("fontSize", speech.FontSize),
                ("message", FormattedMessage.EscapeText(message)));
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Xeno Hivemind chat from {player:Player}: {message}");
        }
        else
        {
            wrappedMessage = Loc.GetString(speech.Bold ? "chat-manager-entity-say-bold-wrap-message" : "chat-manager-entity-say-wrap-message",
                ("entityName", playerName),
                ("verb", Loc.GetString(_random.Pick(speech.SpeechVerbStrings))),
                ("fontType", speech.FontId),
                ("fontSize", 25),
                ("message", FormattedMessage.EscapeText(message)));
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Xeno Hivemind chat from queen {player:Player}: {message}");
        }
        if (HasComp<AlienComponent>(source))
            _chatManager.ChatMessageToMany(ChatChannel.XenoHivemind, message, wrappedMessage, source, hideChat, true, clients.ToList(), author: player.UserId);
    }

#endregion

#region Utility

    private enum MessageRangeCheckResult
    {
        Disallowed,
        HideChat,
        Full
    }

    /// <summary>
    ///     If hideChat should be set as far as replays are concerned.
    /// </summary>
    private bool MessageRangeHideChatForReplay(ChatTransmitRange range)
    {
        return range == ChatTransmitRange.HideChat;
    }

    /// <summary>
    ///     Checks if a target as returned from GetRecipients should receive the message.
    ///     Keep in mind data.Range is -1 for out of range observers.
    /// </summary>
    private MessageRangeCheckResult MessageRangeCheck(
        ICommonSession session,
        ICChatRecipientData data,
        ChatTransmitRange range)
    {
        var initialResult = MessageRangeCheckResult.Full;
        switch (range)
        {
            case ChatTransmitRange.Normal:
                initialResult = MessageRangeCheckResult.Full;
                break;
            case ChatTransmitRange.GhostRangeLimit:
                initialResult = (data.Observer && data.Range < 0 && !_adminManager.IsAdmin(session))
                    ? MessageRangeCheckResult.HideChat
                    : MessageRangeCheckResult.Full;

                break;
            case ChatTransmitRange.HideChat:
                initialResult = MessageRangeCheckResult.HideChat;
                break;
            case ChatTransmitRange.NoGhosts:
                initialResult = (data.Observer && !_adminManager.IsAdmin(session))
                    ? MessageRangeCheckResult.Disallowed
                    : MessageRangeCheckResult.Full;

                break;
        }

        var insistHideChat = data.HideChatOverride ?? false;
        var insistNoHideChat = !(data.HideChatOverride ?? true);
        if (insistHideChat && initialResult == MessageRangeCheckResult.Full)
            return MessageRangeCheckResult.HideChat;

        if (insistNoHideChat && initialResult == MessageRangeCheckResult.HideChat)
            return MessageRangeCheckResult.Full;

        return initialResult;
    }

    /// <summary>
    ///     Sends a chat message to the given players in range of the source entity.
    /// </summary>
    public void SendInVoiceRange(
        ChatChannel channel,
        string message,
        string wrappedMessage,
        EntityUid source,
        ChatTransmitRange range,
        NetUserId? author = null)
    {
        foreach (var (session, data) in GetRecipients(source, VoiceRange))
        {
            var entRange = MessageRangeCheck(session, data, range);
            if (entRange == MessageRangeCheckResult.Disallowed)
                continue;

            var entHideChat = entRange == MessageRangeCheckResult.HideChat;
            _chatManager.ChatMessageToOne(channel, message, wrappedMessage, source, entHideChat, session.Channel,
                author: author);
        }

        _replay.RecordServerMessage(new ChatMessage(channel, message, wrappedMessage, GetNetEntity(source), null,
            MessageRangeHideChatForReplay(range)));
    }

    /// <summary>
    ///     Returns true if the given player is 'allowed' to send the given message, false otherwise.
    /// </summary>
    private bool CanSendInGame(string message, IConsoleShell? shell = null, ICommonSession? player = null)
    {
        // Non-players don't have to worry about these restrictions.
        if (player == null)
            return true;

        var mindContainerComponent = player.ContentData()?.Mind;

        if (mindContainerComponent == null)
        {
            shell?.WriteError("You don't have a mind!");
            return false;
        }

        if (player.AttachedEntity is not { Valid: true } _)
        {
            shell?.WriteError("You don't have an entity!");
            return false;
        }

        return !_chatManager.MessageCharacterLimit(player, message);
    }

    // ReSharper disable once InconsistentNaming
    private string SanitizeInGameICMessage(
        EntityUid source,
        string message,
        out string? emoteStr,
        bool capitalize = true,
        bool punctuate = false,
        bool sanitizeSlang = true)
    {
        var newMessage = message.Trim();

        newMessage = _sanitizer.SanitizeTags(newMessage);

        if (sanitizeSlang)
            newMessage = _sanitizer.SanitizeOutSlang(newMessage);

        if (capitalize)
            newMessage = SanitizeMessageCapital(newMessage);

        if (punctuate)
            newMessage = SanitizeMessagePeriod(newMessage);

        _sanitizer.TrySanitizeOutSmilies(newMessage, source, out newMessage, out emoteStr);

        return newMessage;
    }

    private string SanitizeInGameOOCMessage(string message)
    {
        var newMessage = message.Trim();
        newMessage = FormattedMessage.EscapeText(newMessage);

        return newMessage;
    }

    public string TransformSpeech(EntityUid sender, string message)
    {
        var ev = new TransformSpeechEvent(sender, message);
        RaiseLocalEvent(ev);

        return ev.Message;
    }

    public bool CheckIgnoreSpeechBlocker(EntityUid sender, bool ignoreBlocker)
    {
        if (ignoreBlocker)
            return ignoreBlocker;

        var ev = new CheckIgnoreSpeechBlockerEvent(sender, ignoreBlocker);
        RaiseLocalEvent(sender, ev, true);

        return ev.IgnoreBlocker;
    }

    private IEnumerable<INetChannel> GetDeadChatClients()
    {
        return Filter.Empty()
            .AddWhereAttachedEntity(HasComp<GhostComponent>)
            .Recipients
            .Union(_adminManager.ActiveAdmins)
            .Select(p => p.Channel);
    }

    private IEnumerable<INetChannel> GetXenoChatClients()
    {
        return Filter.Empty()
            .AddWhereAttachedEntity(HasComp<AlienComponent>)
            .Recipients
            .Select(p => p.Channel);
    }

    private string SanitizeMessagePeriod(string message)
    {
        if (string.IsNullOrEmpty(message))
            return message;

        // Adds a period if the last character is a letter.
        if (char.IsLetter(message[^1]))
            message += ".";

        return message;
    }

    [ValidatePrototypeId<ReplacementAccentPrototype>]
    public const string ChatSanitizeAccent = "chatsanitize";

    public string SanitizeMessageReplaceWords(string message)
    {
        if (string.IsNullOrEmpty(message))
            return message;

        var msg = message;

        msg = _wordreplacement.ApplyReplacements(msg, ChatSanitizeAccent);

        return msg;
    }

    /// <summary>
    ///     Returns list of players and ranges for all players withing some range. Also returns observers with a range of -1.
    /// </summary>
    private Dictionary<ICommonSession, ICChatRecipientData> GetRecipients(EntityUid source, float voiceGetRange)
    {
        // TODO proper speech occlusion

        var recipients = new Dictionary<ICommonSession, ICChatRecipientData>();
        var ghostHearing = GetEntityQuery<GhostHearingComponent>();
        var xforms = GetEntityQuery<TransformComponent>();

        var transformSource = xforms.GetComponent(source);
        var sourceMapId = transformSource.MapID;
        var sourceCoords = transformSource.Coordinates;

        foreach (var player in _playerManager.Sessions)
        {
            if (player.AttachedEntity is not { Valid: true } playerEntity)
                continue;

            var transformEntity = xforms.GetComponent(playerEntity);

            if (transformEntity.MapID != sourceMapId)
                continue;

            var observer = ghostHearing.HasComponent(playerEntity);

            // even if they are a ghost hearer, in some situations we still need the range
            if (sourceCoords.TryDistance(EntityManager, transformEntity.Coordinates, out var distance) &&
                distance < voiceGetRange)
            {
                recipients.Add(player, new ICChatRecipientData(distance, observer));
                continue;
            }

            if (observer)
                recipients.Add(player, new ICChatRecipientData(-1, true));
        }

        RaiseLocalEvent(new ExpandICChatRecipientstEvent(source, voiceGetRange, recipients));
        return recipients;
    }

    public readonly record struct ICChatRecipientData(float Range, bool Observer, bool? HideChatOverride = null);

    private string ObfuscateMessageReadability(string message, float chance)
    {
        var modifiedMessage = new StringBuilder(message);

        for (var i = 0; i < message.Length; i++)
        {
            if (char.IsWhiteSpace((modifiedMessage[i])))
            {
                continue;
            }

            if (_random.Prob(1 - chance))
            {
                modifiedMessage[i] = '~';
            }
        }

        return modifiedMessage.ToString();
    }

    public string BuildGibberishString(IReadOnlyList<char> charOptions, int length)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < length; i++)
        {
            sb.Append(_random.Pick(charOptions));
        }

        return sb.ToString();
    }

#endregion
}

/// <summary>
///     This event is raised before chat messages are sent out to clients. This enables some systems to send the chat
///     messages to otherwise out-of view entities (e.g. for multiple viewports from cameras).
/// </summary>
public record ExpandICChatRecipientstEvent(
    EntityUid Source,
    float VoiceRange,
    Dictionary<ICommonSession, ChatSystem.ICChatRecipientData> Recipients);

public sealed class TransformSpeakerNameEvent(EntityUid sender, string name, string? speechVerb = null)
    : EntityEventArgs
{
    public EntityUid Sender = sender;
    public string Name = name;
    public string? SpeechVerb = speechVerb;
}

/// <summary>
///     Raised broadcast in order to transform speech.transmit
/// </summary>
public sealed class TransformSpeechEvent(EntityUid sender, string message) : EntityEventArgs
{
    public EntityUid Sender = sender;
    public string Message = message;
}

public sealed class CheckIgnoreSpeechBlockerEvent(EntityUid sender, bool ignoreBlocker) : EntityEventArgs
{
    public EntityUid Sender = sender;
    public bool IgnoreBlocker = ignoreBlocker;
}

/// <summary>
///     Raised on an entity when it speaks, either through 'say' or 'whisper'.
/// </summary>
public sealed class EntitySpokeEvent(
    EntityUid source,
    string message,
    string originalMessage,
    RadioChannelPrototype? channel,
    string? obfuscatedMessage)
    : EntityEventArgs
{
    public readonly EntityUid Source = source;
    public readonly string Message = message;
    public readonly string OriginalMessage = originalMessage;
    public readonly string? ObfuscatedMessage = obfuscatedMessage; // not null if this was a whisper

    /// <summary>
    ///     If the entity was trying to speak into a radio, this was the channel they were trying to access. If a radio
    ///     message gets sent on this channel, this should be set to null to prevent duplicate messages.
    /// </summary>
    public RadioChannelPrototype? Channel = channel;
}

//WD-EDIT
public sealed class SetSpeakerColorEvent(EntityUid sender, string name)
{
    public EntityUid Sender { get; set; } = sender;

    public string Name { get; set; } = name;
}

public sealed class SpeechTransformedEvent(EntityUid sender, string message) : EntityEventArgs
{
    public EntityUid Sender = sender;
    public string Message = message;
}

//WD-EDIT

/// <summary>
///     InGame IC chat is for chat that is specifically ingame (not lobby) but is also in character, i.e. speaking.
/// </summary>
// ReSharper disable once InconsistentNaming
public enum InGameICChatType : byte
{
    Speak,
    Emote,
    Whisper
}

/// <summary>
///     InGame OOC chat is for chat that is specifically ingame (not lobby) but is OOC, like deadchat or LOOC.
/// </summary>
public enum InGameOOCChatType : byte
{
    Looc,
    Dead,
    Changeling,
    HiveXeno,
    Cult
}

/// <summary>
///     Controls transmission of chat.
/// </summary>
public enum ChatTransmitRange : byte
{
    /// Acts normal, ghosts can hear across the map, etc.
    Normal,

    /// Normal but ghosts are still range-limited.
    GhostRangeLimit,

    /// Hidden from the chat window.
    HideChat,

    /// Ghosts can't hear or see it at all. Regular players can if in-range.
    NoGhosts
}
