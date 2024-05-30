using Content.Shared._White.PolymorphableCanister;
using JetBrains.Annotations;

namespace Content.Client._White.PolymorphableCanister.UI;

[UsedImplicitly]
// ReSharper disable once InconsistentNaming
public sealed class PolymorphableCanisterBUI : BoundUserInterface
{
    private PolymorphableCanisterMenu? _menu;

    public PolymorphableCanisterBUI(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        _menu = new PolymorphableCanisterMenu(Owner, this);
        _menu.OnClose += Close;
        _menu.OpenCentered();
    }

    public void SendMessage(string protoId)
    {
        SendMessage(new PolymorphableCanisterMessage(protoId));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        _menu?.Dispose();
    }
}
