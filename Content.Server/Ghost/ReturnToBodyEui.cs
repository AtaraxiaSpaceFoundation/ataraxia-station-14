using Content.Server.EUI;
using Content.Shared.Eui;
using Content.Shared.Ghost;
using Content.Shared.Mind;

namespace Content.Server.Ghost;

public sealed class ReturnToBodyEui : BaseEui
{
    private readonly SharedMindSystem _mindSystem;

    private readonly MindComponent _mind;

    private readonly EntityUid _mindId;

    private readonly EntityUid? _transferTo;

    public ReturnToBodyEui(MindComponent mind, SharedMindSystem mindSystem)
    {
        _mind = mind;
        _mindSystem = mindSystem;
    }

    // WD START
    public ReturnToBodyEui(MindComponent mind, SharedMindSystem mindSystem, EntityUid mindId, EntityUid? transferTo)
    {
        _mind = mind;
        _mindSystem = mindSystem;
        _mindId = mindId;
        _transferTo = transferTo;
    }
    // WD END

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is not ReturnToBodyMessage choice ||
            !choice.Accepted)
        {
            Close();
            return;
        }

        if (_transferTo != null) // WD
            _mindSystem.TransferTo(_mindId, _transferTo, mind: _mind);

        _mindSystem.UnVisit(_mind.Session);

        Close();
    }
}
