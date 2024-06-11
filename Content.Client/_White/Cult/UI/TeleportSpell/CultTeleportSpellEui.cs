using System.Linq;
using Content.Client._White.Cult.UI.TeleportRunesList;
using Content.Client.Eui;
using Content.Shared.Eui;
using Content.Shared._White.Cult.UI;
using JetBrains.Annotations;

namespace Content.Client._White.Cult.UI.TeleportSpell;

[UsedImplicitly]
public sealed class CultTeleportSpellEui : BaseEui
{
    private readonly TeleportRunesListWindow _window = new();

    public override void Opened()
    {
        _window.OpenCentered();
        _window.ItemSelected += (index, _) => SendMessage(new TeleportSpellTargetRuneSelected {RuneUid = index});
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
        if (state is not CultTeleportSpellEuiState cast)
            return;

        _window.Clear();
        _window.PopulateList(cast.Runes.Keys.ToList(), cast.Runes.Values.ToList());
    }
}
