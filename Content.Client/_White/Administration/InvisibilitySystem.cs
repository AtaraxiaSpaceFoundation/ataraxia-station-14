using Content.Shared._White.Administration;
using Content.Shared.Popups;
using Content.Shared._White.Administration;
using Robust.Client.Console;
using Robust.Client.GameObjects;
using InvisibilityComponent = Content.Shared._White.Administration.InvisibilityComponent;
using ToggleInvisibilityActionEvent = Content.Shared._White.Administration.ToggleInvisibilityActionEvent;

namespace Content.Client._White.Administration;

public sealed class InvisibilitySystem : SharedInvisibilitySystem
{
    [Dependency] private readonly IClientConsoleHost _console = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InvisibilityComponent, ToggleInvisibilityActionEvent>(OnToggleGhosts);
        SubscribeNetworkEvent<InvisibilityToggleEvent>(OnInvisibilityToggle);
    }

    private void OnInvisibilityToggle(InvisibilityToggleEvent ev)
    {
        var ent = GetEntity(ev.Uid);

        if (!EntityManager.TryGetComponent(ent, out SpriteComponent? sprite))
            return;

        var component = EntityManager.EnsureComponent<InvisibilityComponent>(ent);
        component.Invisible = ev.Invisible;
        component.DefaultAlpha ??= sprite.Color.A;

        var newAlpha = ev.Invisible ? component.DefaultAlpha.Value / 3f : component.DefaultAlpha.Value;
        sprite.Color = sprite.Color.WithAlpha(newAlpha);
    }

    private void OnToggleGhosts(EntityUid uid, InvisibilityComponent component, ToggleInvisibilityActionEvent args)
    {
        if (args.Handled)
            return;

        _popup.PopupEntity("Невидимость переключена", args.Performer);
        _console.RemoteExecuteCommand(null, "invisibility");

        args.Handled = true;
    }
}
