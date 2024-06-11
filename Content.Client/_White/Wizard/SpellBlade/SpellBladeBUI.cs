using Content.Client._White.UserInterface.Radial;
using Content.Shared._White.Wizard.SpellBlade;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client._White.Wizard.SpellBlade;

[UsedImplicitly]
// ReSharper disable once InconsistentNaming
public sealed class SpellBladeBUI(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    private RadialContainer? _aspectSelector;

    protected override void Open()
    {
        base.Open();

        if (!_entityManager.TryGetComponent(Owner, out SpellBladeComponent? spellBlade) ||
            spellBlade.ChosenAspect != string.Empty)
            return;

        var spriteSystem = _entityManager.System<SpriteSystem>();
        _aspectSelector = new RadialContainer();

        _aspectSelector.Closed += Close;

        foreach (var aspect in spellBlade.Aspects)
        {
            if (!_prototypeManager.TryIndex(aspect, out var proto))
                continue;

            var button = _aspectSelector.AddButton(proto.Name,
                spriteSystem.GetPrototypeIcon(proto).Default);
            button.Tooltip = proto.Description;

            button.Controller.OnPressed += _ =>
            {
                SendMessage(new SpellBladeSystemMessage(aspect));
                _aspectSelector.Close();
            };

        }

        _aspectSelector.OpenAttachedLocalPlayer();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        _aspectSelector?.Close();
    }
}
