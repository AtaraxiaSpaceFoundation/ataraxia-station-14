using System.Linq;
using Content.Client._White.Cult.UI.TeleportRunesList;
using Content.Client.Eui;
using Content.Shared._White.Wizard.Teleport;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Client._White.Wizard.TeleportSpell;

[UsedImplicitly]
public sealed class WizardTeleportSpellEui : BaseEui
{
    private readonly TeleportRunesListWindow _window = new();

    public override void Opened()
    {
        _window.OpenCentered();
        _window.ItemSelected +=
            (index, _) => SendMessage(new TeleportSpellTargetLocationSelected {LocationUid = index});
        _window.OnClose += () => SendMessage(new CloseEuiMessage());

        base.Opened();
    }

    public override void Closed()
    {
        base.Closed();
        _window.Close();
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not WizardTeleportSpellEuiState cast)
            return;

        _window.Clear();
        _window.PopulateList(cast.Locations.Keys.ToList(), cast.Locations.Values.ToList());
    }
}
