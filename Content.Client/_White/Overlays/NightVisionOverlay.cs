using Content.Shared._White.Overlays;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client._White.Overlays
{
    public sealed class NightVisionOverlay : Overlay
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public override bool RequestScreenTexture => true;

        public override OverlaySpace Space => OverlaySpace.WorldSpace;

        private readonly ShaderInstance _shader;

        public NightVisionOverlay()
        {
            IoCManager.InjectDependencies(this);
            _shader = _prototypeManager.Index<ShaderPrototype>("NightVision").InstanceUnique();
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            if (ScreenTexture == null)
                return;

            var handle = args.WorldHandle;

            Color? color = null;

            if (_entityManager.TryGetComponent<NightVisionComponent>(_playerManager.LocalSession?.AttachedEntity,
                    out var component) && component.IsActive)
            {
                _shader.SetParameter("tint", component.Tint);
                _shader.SetParameter("luminance_threshold", component.Strength);
                _shader.SetParameter("noise_amount", component.Noise);
                color = component.Color;
            }
            else if (_entityManager.TryGetComponent<TemporaryNightVisionComponent>(
                         _playerManager.LocalSession?.AttachedEntity, out var tempNvComp))
            {
                _shader.SetParameter("tint", tempNvComp.Tint);
                _shader.SetParameter("luminance_threshold", tempNvComp.Strength);
                _shader.SetParameter("noise_amount", tempNvComp.Noise);
                color = tempNvComp.Color;
            }

            if (color == null)
                return;

            _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);

            handle.UseShader(_shader);
            handle.DrawRect(args.WorldBounds, color.Value);
            handle.UseShader(null);
        }
    }
}
