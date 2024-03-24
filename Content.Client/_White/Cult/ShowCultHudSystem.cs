﻿using Content.Shared._White.Cult;
using Content.Shared._White.Cult.Components;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;
using CultistComponent = Content.Shared._White.Cult.Components.CultistComponent;

namespace Content.Client._White.Cult;

public sealed class ShowCultHudSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlayManager = default!;

    private Overlay _overlay = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<CultistComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<CultistComponent, ComponentRemove>(OnComponentRemoved);
        SubscribeLocalEvent<CultistComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<CultistComponent, PlayerDetachedEvent>(OnPlayerDetached);

        SubscribeLocalEvent<ShowCultHudComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<ShowCultHudComponent, ComponentRemove>(OnComponentRemoved);
        SubscribeLocalEvent<ShowCultHudComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<ShowCultHudComponent, PlayerDetachedEvent>(OnPlayerDetached);

        _overlay = new CultHudOverlay(EntityManager);
    }

    private void OnComponentInit(EntityUid uid, ShowCultHudComponent component, ComponentInit args)
    {
        if (_player.LocalSession?.AttachedEntity != uid)
            return;

        _overlayManager.AddOverlay(_overlay);

    }

    private void OnComponentRemoved(EntityUid uid, ShowCultHudComponent component, ComponentRemove args)
    {
        if (_player.LocalSession?.AttachedEntity != uid)
            return;

        _overlayManager.RemoveOverlay(_overlay);

    }

    private void OnPlayerAttached(EntityUid uid, ShowCultHudComponent component, PlayerAttachedEvent args)
    {
        if (_player.LocalSession != args.Player)
            return;

        _overlayManager.AddOverlay(_overlay);
    }

    private void OnPlayerDetached(EntityUid uid, ShowCultHudComponent component, PlayerDetachedEvent args)
    {
        if (_player.LocalSession != args.Player)
            return;

        _overlayManager.RemoveOverlay(_overlay);
    }
}