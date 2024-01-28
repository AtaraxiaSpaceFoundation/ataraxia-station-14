using Content.Shared.GameTicking;
using Content.Shared._White.EntityHealthBar;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client._White.EntityHealthBar
{
    public sealed class ShowHealthBarsSystem : EntitySystem
    {
        [Dependency] private readonly IPlayerManager _player = default!;
        [Dependency] private readonly IOverlayManager _overlayMan = default!;

        private EntityHealthBarOverlay _overlay = default!;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ShowWhiteHealthBarsComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<ShowWhiteHealthBarsComponent, ComponentRemove>(OnRemove);
            SubscribeLocalEvent<ShowWhiteHealthBarsComponent, PlayerAttachedEvent>(OnPlayerAttached);
            SubscribeLocalEvent<ShowWhiteHealthBarsComponent, PlayerDetachedEvent>(OnPlayerDetached);
            SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);

            _overlay = new(EntityManager);
        }

        private void OnInit(EntityUid uid, ShowWhiteHealthBarsComponent component, ComponentInit args)
        {
            if (_player.LocalPlayer?.ControlledEntity == uid)
            {
                _overlayMan.AddOverlay(_overlay);
                _overlay.DamageContainers = component.DamageContainers;
            }


        }
        private void OnRemove(EntityUid uid, ShowWhiteHealthBarsComponent component, ComponentRemove args)
        {
            if (_player.LocalPlayer?.ControlledEntity == uid)
            {
                _overlayMan.RemoveOverlay(_overlay);
            }
        }

        private void OnPlayerAttached(EntityUid uid, ShowWhiteHealthBarsComponent component, PlayerAttachedEvent args)
        {
            _overlayMan.AddOverlay(_overlay);
            _overlay.DamageContainers = component.DamageContainers;
        }

        private void OnPlayerDetached(EntityUid uid, ShowWhiteHealthBarsComponent component, PlayerDetachedEvent args)
        {
            _overlayMan.RemoveOverlay(_overlay);
        }

        private void OnRoundRestart(RoundRestartCleanupEvent args)
        {
            _overlayMan.RemoveOverlay(_overlay);
        }
    }
}
