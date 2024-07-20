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

            if (_playerManager.LocalEntity == null)
                return;

            var uid = _playerManager.LocalEntity.Value;

            Color? color = null;

            if (_entityManager.TryGetComponent<NightVisionComponent>(uid, out var component) && component.IsActive)
            {
                color = SetParameters(component);
            }
            else if (_entityManager.TryGetComponent<ThermalVisionComponent>(uid, out var thermal) && thermal.IsActive)
            {
                color = SetParameters(thermal);
            }
            else if (_entityManager.TryGetComponent<TemporaryNightVisionComponent>(uid, out var tempNvComp))
            {
                color = SetParameters(tempNvComp);
            }
            else if (_entityManager.TryGetComponent<TemporaryThermalVisionComponent>(uid, out var tempThermal))
            {
                color = SetParameters(tempThermal);
            }

            if (color == null)
                return;

            _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);

            handle.UseShader(_shader);
            handle.DrawRect(args.WorldBounds, color.Value);
            handle.UseShader(null);
        }

        private Color SetParameters(BaseNvOverlayComponent component)
        {
            _shader.SetParameter("tint", component.Tint);
            _shader.SetParameter("luminance_threshold", component.Strength);
            _shader.SetParameter("noise_amount", component.Noise);
            return component.Color;
        }
    }
}
