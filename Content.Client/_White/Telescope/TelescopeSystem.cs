using System.Numerics;
using Content.Client.Viewport;
using Content.Shared._White.Telescope;
using Content.Shared.Hands.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using Robust.Shared.Timing;

namespace Content.Client._White.Telescope;

public sealed class TelescopeSystem : SharedTelescopeSystem
{
    [Dependency] private readonly InputSystem _inputSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;

    private ScalingViewport? _viewport;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.ApplyingState || !_timing.IsFirstTimePredicted || !_input.MouseScreenPosition.IsValid)
            return;

        var player = _player.LocalEntity;

        if (!TryComp<HandsComponent>(player, out var hands) ||
            !TryComp<TelescopeComponent>(hands.ActiveHandEntity, out var telescope) ||
            !TryComp<EyeComponent>(player.Value, out var eye))
            return;

        var offset = Vector2.Zero;

        if (_inputSystem.CmdStates.GetState(EngineKeyFunctions.UseSecondary) != BoundKeyState.Down)
        {
            RaisePredictiveEvent(new EyeOffsetChangedEvent
            {
                Offset = offset
            });
            return;
        }

        var mousePos = _input.MouseScreenPosition;

        if (_uiManager.MouseGetControl(mousePos) as ScalingViewport is { } viewport)
            _viewport = viewport;

        if (_viewport == null)
            return;

        var centerPos = _eyeManager.WorldToScreen(eye.Eye.Position.Position + eye.Offset);

        var diff = mousePos.Position - centerPos;
        var len = diff.Length();

        var size = _viewport.PixelSize;

        var maxLength = Math.Min(size.X, size.Y) * 0.4f;
        var minLength = maxLength * 0.2f;

        if (len > maxLength)
        {
            diff *= maxLength / len;
            len = maxLength;
        }

        var divisor = maxLength * telescope.Divisor;

        if (len > minLength)
        {
            diff -= diff * minLength / len;
            offset = new Vector2(diff.X / divisor, -diff.Y / divisor);
            offset = new Angle(-eye.Rotation.Theta).RotateVec(offset);
        }

        RaisePredictiveEvent(new EyeOffsetChangedEvent
        {
            Offset = offset
        });
    }
}
