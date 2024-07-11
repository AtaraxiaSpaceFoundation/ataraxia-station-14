using Content.Server.Chat.Systems;
using Content.Shared.Random.Helpers;
using Robust.Shared.Random;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators;

public sealed partial class SpeakOperator : HTNOperator
{
    private ChatSystem _chat = default!;
    private IRobustRandom _random = default!;

    [DataField("speech", required: true)]
    public List<string> Speech { get; set; } = new();

    /// <summary>
    /// Whether to hide message from chat window and logs.
    /// </summary>
    [DataField]
    public bool Hidden;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _chat = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<ChatSystem>();
        _random = IoCManager.Resolve<IRobustRandom>();
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var speaker = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var message = Loc.GetString(_random.Pick(Speech));

        _chat.TrySendInGameICMessage(speaker, message, InGameICChatType.Speak, hideChat: Hidden, hideLog: Hidden);
        return base.Update(blackboard, frameTime);
    }
}
