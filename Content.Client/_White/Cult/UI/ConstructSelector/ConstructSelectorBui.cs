using System.Linq;
using Content.Client._White.UserInterface.Radial;
using Content.Shared._White.Cult.UI;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;
using ConstructShellComponent = Content.Shared._White.Cult.Components.ConstructShellComponent;

namespace Content.Client._White.Cult.UI.ConstructSelector;

public sealed class ConstructSelectorBui : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    private SpriteSystem _spriteSystem = default!;

    private bool _selected;

    public ConstructSelectorBui(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _spriteSystem = _entityManager.EntitySysManager.GetEntitySystem<SpriteSystem>();
        var shellComponent = _entityManager.GetComponent<ConstructShellComponent>(Owner);

        var shellSelector = new RadialContainer();

        shellSelector.Closed += () =>
        {
            if (_selected)
                return;

            SendMessage(new ConstructFormSelectedEvent(shellComponent.ConstructForms.First()));
        };

        foreach (var form in shellComponent.ConstructForms)
        {
            var formPrototype = _prototypeManager.Index<EntityPrototype>(form);
            var button = shellSelector.AddButton(formPrototype.Name,
                _spriteSystem.GetPrototypeIcon(formPrototype).Default);

            button.Controller.OnPressed += _ =>
            {
                _selected = true;
                SendMessage(new ConstructFormSelectedEvent(form));
                shellSelector.Close();
            };
        }

        shellSelector.OpenCentered();
    }
}
