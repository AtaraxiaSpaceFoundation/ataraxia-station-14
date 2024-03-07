using Content.Client.Actions;
using Content.Shared._White.Wizard;
using Content.Shared._White.Wizard.Charging;
using Content.Shared.Actions;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client.UserInterface.Systems.Actions;

public sealed class ChargeActionSystem : SharedChargingSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly ActionsSystem _actionsSystem = default!;
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;
    [Dependency] private readonly InputSystem _inputSystem = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;

    private ActionUIController? _controller;

    public event Action<bool>? ChargingUpdated;

    private bool _charging;
    private bool _prevCharging;

    private float _chargeTime;
    private int _chargeLevel;
    private int _prevChargeLevel;

    private bool _isChargingPlaying;
    private bool _isChargedPlaying;

    private const float LevelChargeTime = 1.5f;

    public override void Initialize()
    {
        base.Initialize();

        _controller = _uiManager.GetUIController<ActionUIController>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_playerManager.LocalEntity is not { } user)
            return;

        if (!_timing.IsFirstTimePredicted || _controller == null || _controller.SelectingTargetFor is not { } actionId)
            return;

        if (!_actionsSystem.TryGetActionData(actionId, out var baseAction) ||
            baseAction is not BaseTargetActionComponent action || !action.IsChargeEnabled)
            return;

        if (!action.Enabled
            || action is { Charges: 0, RenewCharges: false }
            || action.Cooldown.HasValue && action.Cooldown.Value.End > _timing.CurTime)
        {
            return;
        }

        var altDown = _inputSystem.CmdStates.GetState(EngineKeyFunctions.UseSecondary);
        switch (altDown)
        {
            case BoundKeyState.Down:
                _prevCharging = _charging;
                _charging = true;
                _chargeTime += frameTime;
                _chargeLevel = (int) (_chargeTime / LevelChargeTime) + 1;
                _chargeLevel = Math.Clamp(_chargeLevel, 1, action.MaxChargeLevel);
                break;
            case BoundKeyState.Up when _charging:
                _prevCharging = _charging;
                _charging = false;
                _chargeTime = 0f;
                _isChargingPlaying = false;
                _isChargedPlaying = false;

                HandleAction(actionId, action, user, _chargeLevel);
                _chargeLevel = 0;

                RaiseNetworkEvent(new RequestAudioSpellStop());
                RaiseNetworkEvent(new RemoveWizardChargeEvent());
                break;
            case BoundKeyState.Up:
                _prevCharging = _charging;
                _chargeLevel = 0;
                _charging = false;
                _chargeTime = 0f;
                _isChargingPlaying = false;
                _isChargedPlaying = false;

                RaiseNetworkEvent(new RequestAudioSpellStop());
                RaiseNetworkEvent(new RemoveWizardChargeEvent());
                break;
        }

        if (_chargeLevel != _prevChargeLevel)
        {
            if (_chargeLevel > 0 && _charging)
            {
                RaiseNetworkEvent(new AddWizardChargeEvent(action.ChargeProto));
            }
            _prevChargeLevel = _chargeLevel;
        }

        if (_prevCharging != _charging)
        {
            ChargingUpdated?.Invoke(_charging);
        }

        if (_charging && !_isChargingPlaying)
        {
            _isChargingPlaying = true;
            RaiseNetworkEvent(new RequestSpellChargingAudio(action.ChargingSound, action.LoopCharging));
        }

        if (_chargeLevel >= action.MaxChargeLevel && !_isChargedPlaying && _charging)
        {
            _isChargedPlaying = true;
            RaiseNetworkEvent(new RequestSpellChargedAudio(action.MaxChargedSound, action.LoopMaxCharged));
        }
    }

    private void HandleAction(EntityUid actionId, BaseTargetActionComponent action, EntityUid user, int chargeLevel)
    {
        var mousePos = _eyeManager.PixelToMap(_inputManager.MouseScreenPosition);
        if (mousePos.MapId == MapId.Nullspace)
            return;

        var coordinates = EntityCoordinates.FromMap(_mapManager.TryFindGridAt(mousePos, out var gridUid, out _)
            ? gridUid
            : _mapManager.GetMapEntityId(mousePos.MapId), mousePos, _transformSystem, EntityManager);

        if (!EntityManager.TryGetComponent(user, out ActionsComponent? comp))
            return;

        switch (action)
        {
            case WorldTargetActionComponent mapTarget:
                _controller?.TryTargetWorld(coordinates, actionId, mapTarget, user, comp, ActionUseType.Charge, chargeLevel);
                break;
        }

        RaiseNetworkEvent(new RequestAudioSpellStop());
        RaiseNetworkEvent(new RemoveWizardChargeEvent());
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _controller = null;

        _charging = false;
        _prevCharging = false;
        _chargeTime = 0f;
        _chargeLevel = 0;
        _prevChargeLevel = 0;
        _isChargingPlaying = false;
        _isChargedPlaying = false;
    }
}
