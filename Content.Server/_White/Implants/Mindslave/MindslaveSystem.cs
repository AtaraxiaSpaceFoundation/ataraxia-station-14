using Content.Server.Chat.Managers;
using Content.Server.Roles;
using Content.Server.Roles.Jobs;
using Content.Shared._White.Implants.Mindslave;
using Content.Shared._White.Implants.Mindslave.Components;
using Content.Shared.Chat;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;

namespace Content.Server._White.Implants.Mindslave;

public sealed class MindslaveSystem : SharedMindslaveSystem
{
    [Dependency] private readonly RoleSystem _role = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly JobSystem _job = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SubdermalImplantComponent, SubdermalImplantInserted>(OnMindslaveInserted);
        SubscribeLocalEvent<SubdermalImplantComponent, SubdermalImplantRemoved>(OnMindslaveRemoved);
    }

    private void OnMindslaveInserted(Entity<SubdermalImplantComponent> ent, ref SubdermalImplantInserted args)
    {
        if (!Tag.HasTag(ent.Owner, MindslaveTag))
        {
            return;
        }

        var slaveComponent = EnsureComp<MindSlaveComponent>(args.Target);
        slaveComponent.Slaves.Add(GetNetEntity(args.Target));
        slaveComponent.Master = GetNetEntity(args.User);

        var masterComponent = EnsureComp<MindSlaveComponent>(args.User);
        masterComponent.Slaves.Add(GetNetEntity(args.Target));
        masterComponent.Master = GetNetEntity(args.User);

        Dirty(args.Target, masterComponent);

        if (!Mind.TryGetMind(args.Target, out var targetMindId, out var targetMind) || targetMind.Session is null)
        {
            return;
        }

        var jobName = _job.MindTryGetJobName(args.User);

        // send message to chat
        var message = Loc.GetString("mindslave-chat-message", ("player", args.User), ("role", jobName));
        var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
        _chatManager.ChatMessageToOne(ChatChannel.Server, message, wrappedMessage, default, false,
            targetMind.Session.Channel, Color.FromHex("#5e9cff"));

        // add briefing in character menu
        if (TryComp<RoleBriefingComponent>(targetMindId, out var roleBriefing))
        {
            roleBriefing.Briefing += Loc.GetString("mindslave-briefing", ("player", args.User), ("role", jobName));
            Dirty(targetMindId, roleBriefing);
        }
        else
        {
            _role.MindAddRole(targetMindId, new RoleBriefingComponent
            {
                Briefing = Loc.GetString("mindslave-briefing", ("player", args.User), ("role", jobName))
            }, targetMind);
        }
    }

    private void OnMindslaveRemoved(Entity<SubdermalImplantComponent> ent, ref SubdermalImplantRemoved args)
    {
        if (!TryComp(args.Target, out MindSlaveComponent? mindslave))
        {
            return;
        }

        if (Mind.TryGetMind(args.Target, out var mindId, out _))
        {
            _role.MindTryRemoveRole<RoleBriefingComponent>(mindId);
            Popup.PopupEntity(Loc.GetString("mindslave-freed", ("player", mindslave.Master)), args.Target, args.Target);
        }

        RemComp<MindSlaveComponent>(args.Target);
    }
}