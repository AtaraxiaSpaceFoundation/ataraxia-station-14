using System.Numerics;
using Content.Shared._White.Telescope;
using Content.Shared.Hands.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
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

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.ApplyingState || !_timing.IsFirstTimePredicted || !_input.MouseScreenPosition.IsValid)
            return;

        var player = _player.LocalEntity;

        if (!TryComp<HandsComponent>(player, out var hands) ||
            !TryComp<TelescopeComponent>(hands.ActiveHandEntity, out var telescope) ||
            !TryComp<EyeComponent>(player.Value, out var eye) || !TryComp(player.Value, out TransformComponent? xform))
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

        var mousePos = _input.MouseScreenPosition.Position;
        var playerPos = _eyeManager.CoordinatesToScreen(xform.Coordinates).Position;

        var diff = mousePos - playerPos;
        var len = diff.Length();

        if (len > telescope.MaxLength)
        {
            diff *= telescope.MaxLength / len;
            len = telescope.MaxLength;
        }

        if (len > telescope.MinLength)
        {
            diff -= diff * telescope.MinLength / len;
            offset = new Vector2(diff.X / telescope.Divisor, -diff.Y / telescope.Divisor);
            offset = new Angle(-eye.Rotation.Theta).RotateVec(offset);
        }

        RaisePredictiveEvent(new EyeOffsetChangedEvent
        {
            Offset = offset
        });
    }
}
