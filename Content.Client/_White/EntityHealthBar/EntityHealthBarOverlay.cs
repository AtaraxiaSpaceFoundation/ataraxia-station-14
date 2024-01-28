using System.Numerics;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._White.EntityHealthBar;

/// <summary>
/// Yeah a lot of this is duplicated from doafters.
/// Not much to be done until there's a generic HUD system
/// </summary>
public sealed class EntityHealthBarOverlay : Overlay
{
    private readonly IEntityManager _entManager;
    private readonly SharedTransformSystem _transform;
    private readonly MobStateSystem _mobStateSystem;
    private readonly MobThresholdSystem _mobThresholdSystem;
    private readonly Texture _barTexture;
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;
    public List<string>? DamageContainers;
    // for icon frame change timer
    private int _iconFrame = 1;
    private const double DelayTime = 0.25;

    public EntityHealthBarOverlay(IEntityManager entManager)
    {
        _entManager = entManager;
        _transform = _entManager.EntitySysManager.GetEntitySystem<SharedTransformSystem>();
        _mobStateSystem = _entManager.EntitySysManager.GetEntitySystem<MobStateSystem>();
        _mobThresholdSystem = _entManager.EntitySysManager.GetEntitySystem<MobThresholdSystem>();

        var sprite = new SpriteSpecifier.Rsi(new ResPath("/Textures/Interface/Misc/health_status.rsi"), "background");
        _barTexture = _entManager.EntitySysManager.GetEntitySystem<SpriteSystem>().Frame0(sprite);

        Timer.SpawnRepeating(TimeSpan.FromSeconds(DelayTime), () =>
        {
            if (_iconFrame < 8)
                _iconFrame++;
            else
                _iconFrame = 1;
        }, new System.Threading.CancellationToken());
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;
        var rotation = args.Viewport.Eye?.Rotation ?? Angle.Zero;
        var spriteQuery = _entManager.GetEntityQuery<SpriteComponent>();
        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();

        var spriteSys = _entManager.EntitySysManager.GetEntitySystem<SpriteSystem>();

        const float scale = 1f;
        var scaleMatrix = Matrix3.CreateScale(new Vector2(scale, scale));
        var rotationMatrix = Matrix3.CreateRotation(-rotation);

        var query = _entManager.EntityQueryEnumerator<MobStateComponent, DamageableComponent, MobThresholdsComponent>();

        while (query.MoveNext(out var uid, out var mob, out var dmg, out var thresholds))
        {
            if (!xformQuery.TryGetComponent(uid, out var xform) ||
                xform.MapID != args.MapId)
            {
                continue;
            }

            if (dmg.DamageContainerID == null || DamageContainers != null && !DamageContainers.Contains(dmg.DamageContainerID))
                continue;

            var worldPosition = _transform.GetWorldPosition(xform);
            var worldMatrix = Matrix3.CreateTranslation(worldPosition);

            Matrix3.Multiply(scaleMatrix, worldMatrix, out var scaledWorld);
            Matrix3.Multiply(rotationMatrix, scaledWorld, out var matty);

            handle.SetTransform(matty);

            // Use the sprite itself if we know its bounds. This means short or tall sprites don't get overlapped
            // by the bar.
            float yOffset;
            float xIconOffset;
            float yIconOffset;
            if (spriteQuery.TryGetComponent(uid, out var sprite))
            {
                yOffset = sprite.Bounds.Height + 12f;
                yIconOffset = sprite.Bounds.Height + 7f;
                xIconOffset = sprite.Bounds.Width + 7f;
            }
            else
            {
                yOffset = 1f;
                yIconOffset = 1f;
                xIconOffset = 1f;
            }

            // Position above the entity (we've already applied the matrix transform to the entity itself)
            // Offset by the texture size for every do_after we have.
            var position = new Vector2(-_barTexture.Width / 2f / EyeManager.PixelsPerMeter,
                yOffset / EyeManager.PixelsPerMeter);

            // Draw the underlying bar texture
            if (sprite is {ContainerOccluded: false})
                handle.DrawTexture(_barTexture, position);
            else
                continue;

            // Draw state icon
            if (dmg.DamageContainerID == "Biological")
            {
                string currentState;
                if (_mobStateSystem.IsAlive(uid, mob))
                {
                    currentState = "life_state";
                }
                else
                {
                    if (_mobStateSystem.IsCritical(uid, mob) &&
                        _mobThresholdSystem.TryGetThresholdForState(uid, MobState.Critical,
                            out var _, thresholds))
                        currentState = "defib_state";
                    else
                        currentState = "dead_state";
                }

                var iconSprite = new SpriteSpecifier.Rsi(new ResPath("/Textures/Interface/Misc/health_state.rsi"),
                    currentState);
                var stateIcon = spriteSys.RsiStateLike(iconSprite)
                    .GetFrame(0, GetIconFrame(spriteSys.RsiStateLike(iconSprite)));

                var iconPosition = new Vector2(xIconOffset / EyeManager.PixelsPerMeter,
                    yIconOffset / EyeManager.PixelsPerMeter);

                handle.DrawTexture(stateIcon, iconPosition);
            }

            // we are all progressing towards death every day
            (float ratio, bool inCrit) deathProgress = CalcProgress(uid, mob, dmg, thresholds);

            var color = GetProgressColor(deathProgress.ratio, deathProgress.inCrit);

            // Hardcoded width of the progress bar because it doesn't match the texture.
            const float startX = 1f;
            const float endX = 15f;

            var xProgress = (endX - startX) * deathProgress.ratio + startX;

            var box = new Box2(new Vector2(startX, 0f) / EyeManager.PixelsPerMeter, new Vector2(xProgress, 2f) / EyeManager.PixelsPerMeter);
            box = box.Translated(position);
            handle.DrawRect(box, color);
        }

        handle.UseShader(null);
        handle.SetTransform(Matrix3.Identity);
    }

    private int GetIconFrame(IRsiStateLike sprite)
    {
        // var _spriteSys = _entManager.EntitySysManager.GetEntitySystem<SpriteSystem>();

        if (sprite.AnimationFrameCount <= 1)
            return 0;

        var currentFrame = _iconFrame;
        int result;
        while (true)
        {
            if (currentFrame > 0 && currentFrame > sprite.AnimationFrameCount)
            {
                currentFrame -= sprite.AnimationFrameCount;
            }
            else
            {
                result = currentFrame - 1;
                break;
            }
        }
        return result;
    }

    /// <summary>
    /// Returns a ratio between 0 and 1, and whether the entity is in crit.
    /// </summary>
    private (float, bool) CalcProgress(EntityUid uid, MobStateComponent component, DamageableComponent dmg, MobThresholdsComponent thresholds)
    {
        if (_mobStateSystem.IsAlive(uid, component))
        {
            if (!_mobThresholdSystem.TryGetThresholdForState(uid, MobState.Critical, out var threshold, thresholds))
                return (1, false);

            var ratio = 1 - ((FixedPoint2) (dmg.TotalDamage / threshold)).Float();
            return (ratio, false);
        }

        if (_mobStateSystem.IsCritical(uid, component))
        {
            if (!_mobThresholdSystem.TryGetThresholdForState(uid, MobState.Critical, out var critThreshold, thresholds) ||
                !_mobThresholdSystem.TryGetThresholdForState(uid, MobState.Dead, out var deadThreshold, thresholds))
            {
                return (1, true);
            }

            var ratio = 1 -
                    ((dmg.TotalDamage - critThreshold) /
                    (deadThreshold - critThreshold)).Value.Float();

            return (ratio, true);
        }

        return (0, true);
    }

    public static Color GetProgressColor(float progress, bool crit)
    {
        if (progress >= 1.0f)
        {
            return new Color(0f, 1f, 0f);
        }
        // lerp
        if (!crit)
        {
            var hue = (5f / 18f) * progress;
            return Color.FromHsv((hue, 1f, 0.75f, 1f));
        }
        else
        {
            return Color.Red;
        }
    }
}
