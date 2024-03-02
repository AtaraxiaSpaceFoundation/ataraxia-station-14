using Content.Shared._White.Cult.Components;
using Content.Shared._White.Implants.Mindslave.Components;
using Content.Shared.Implants;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mindshield.Components;
using Content.Shared.Popups;
using Content.Shared.Tag;

namespace Content.Shared._White.Implants.Mindslave;

public abstract class SharedMindslaveSystem : EntitySystem
{
    [Dependency] protected readonly TagSystem Tag = default!;
    [Dependency] protected readonly SharedMindSystem Mind = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;

    protected const string MindslaveTag = "MindSlave";

    public override void Initialize()
    {
        SubscribeLocalEvent<MindContainerComponent, AddImplantAttemptEvent>(OnTryInsertMindslave);
    }

    private void OnTryInsertMindslave(Entity<MindContainerComponent> ent, ref AddImplantAttemptEvent args)
    {
        if (!Tag.HasTag(args.Implant, MindslaveTag))
        {
            return;
        }

        string message;
        string wrappedMessage;
        if (args.Target == args.User)
        {
            message = Loc.GetString("mindslave-target-self");
            wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
            Popup.PopupClient(wrappedMessage, args.Implanter, args.User);
            args.Cancel();
            return;
        }

        if (HasComp<MindShieldComponent>(args.Target) ||
            HasComp<MindSlaveComponent>(args.Target) ||
            HasComp<CultistComponent>(args.Target) ||
            HasComp<Revolutionary.Components.RevolutionaryComponent>(args.Target))
        {
            message = Loc.GetString("mindslave-cant-insert");
            wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
            Popup.PopupClient(wrappedMessage, args.Implanter, args.User);
            args.Cancel();
        }
    }
}
