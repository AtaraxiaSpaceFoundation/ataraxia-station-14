﻿using Content.Shared.Popups;
using Content.Shared._White.Jukebox;

namespace Content.Client._White.Jukebox;

public sealed class TapeCreatorBUI : BoundUserInterface
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    private readonly SharedPopupSystem _sharedPopupSystem = default!;

    private TapeCreatorMenu? _window;

    public TapeCreatorBUI(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
        _sharedPopupSystem = _entityManager.System<SharedPopupSystem>();

        if (!_entityManager.TryGetComponent<TapeCreatorComponent>(owner, out var tapeCreatorComponent))
        {
            _sharedPopupSystem.PopupEntity($"Тут нет TapeCreatorComponent, звоните кодерам", owner);
            return;
        }

        _window = new TapeCreatorMenu(tapeCreatorComponent);
    }

    protected override void Open()
    {
        base.Open();

        if (_window == null)
        {
            Close();
            return;
        }

        _window.OpenCentered();
        _window.OnClose += Close;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;

        _window?.Dispose();
    }
}
