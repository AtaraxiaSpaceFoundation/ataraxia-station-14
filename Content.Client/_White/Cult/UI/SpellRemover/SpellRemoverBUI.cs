using System.Linq;
using Content.Client._White.UserInterface.Radial;
using Content.Shared._White.Cult;
using Content.Shared.Actions;
using Content.Shared._White.Cult.Components;
using Robust.Client.Player;
using Robust.Client.Utility;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using CultistComponent = Content.Shared._White.Cult.Components.CultistComponent;

namespace Content.Client._White.Cult.UI.SpellRemover;

public sealed class SpellRemoverBUI : BoundUserInterface
{
    private RadialContainer? _radialContainer;

    public SpellRemoverBUI(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();
        _radialContainer = new RadialContainer();
        _radialContainer.Closed += Close;
        var entityManager = IoCManager.Resolve<IEntityManager>();
        var ent = IoCManager.Resolve<IPlayerManager>().LocalEntity;

        if (ent == null || !entityManager.TryGetComponent(ent, out CultistComponent? comp) ||
            comp.SelectedEmpowers.Count == 0)
        {
            Close();
            return;
        }

        var metaQuery = entityManager.GetEntityQuery<MetaDataComponent>();
        var instantQuery = entityManager.GetEntityQuery<InstantActionComponent>();
        var targetQuery = entityManager.GetEntityQuery<EntityTargetActionComponent>();

        foreach (var action in comp.SelectedEmpowers)
        {
            if (action == null)
                continue;

            var spell = entityManager.GetEntity(action.Value);

            if (!entityManager.EntityExists(spell))
                continue;

            SpriteSpecifier? icon;
            if (instantQuery.TryGetComponent(spell, out var instantComp))
                icon = instantComp.Icon;
            else
            {
                if (!targetQuery.TryGetComponent(spell, out var targetComp))
                    continue;
                icon = targetComp.Icon;
            }

            if (icon == null)
                continue;

            var texture = icon.Frame0();

            if (!metaQuery.TryGetComponent(spell, out var meta))
                continue;

            var button = _radialContainer.AddButton(meta.EntityName, texture);

            button.Controller.OnPressed += _ =>
            {
                SendMessage(new CultEmpowerRemoveBuiMessage(action.Value));
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
