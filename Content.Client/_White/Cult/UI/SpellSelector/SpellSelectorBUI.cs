using Content.Client._White.UserInterface.Radial;
using Content.Shared.Actions;
using Content.Shared._White.Cult;
using Content.Shared._White.Cult.Components;
using Robust.Client.Utility;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using CultistComponent = Content.Shared._White.Cult.Components.CultistComponent;

namespace Content.Client._White.Cult.UI.SpellSelector;

public sealed class SpellSelectorBUI : BoundUserInterface
{

    private RadialContainer? _radialContainer;

    public SpellSelectorBUI(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();
        _radialContainer = new RadialContainer();
        _radialContainer.Closed += Close;

        var protoMan = IoCManager.Resolve<IPrototypeManager>();

        foreach (var action in CultistComponent.CultistActions)
        {
            if (!protoMan.TryIndex(action, out var proto))
                continue;

            SpriteSpecifier? icon;
            if (action.StartsWith("InstantAction") && proto.TryGetComponent(out InstantActionComponent? instantComp))
                icon = instantComp.Icon;
            else
            {
                if (!proto.TryGetComponent(out EntityTargetActionComponent? targetComp))
                    continue;
                icon = targetComp.Icon;
            }

            if (icon == null)
                continue;

            var texture = icon.Frame0();
            var button = _radialContainer.AddButton(proto.Name, texture);

            button.Controller.OnPressed += _ =>
            {
                SendMessage(new CultEmpowerSelectedBuiMessage(action));
                Close();
            };
        }

        _radialContainer.OpenAttachedLocalPlayer();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _radialContainer?.Close();
    }
}
