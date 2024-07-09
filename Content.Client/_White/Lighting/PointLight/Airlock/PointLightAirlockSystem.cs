using Content.Shared._White.Lighting.PointLight.Airlock;
using Content.Shared.Doors.Components;
using Robust.Client.GameObjects;

namespace Content.Client._White.Lighting.PointLight.Airlock;

// TODO: Перепилить емаггинг дверей, потому что ебучие двери вообще не в курсе, емагнуты они или нет.
// А сам емаг просто ставит стейты, а не поражает дверь
// А еще он ставит дверь на болты каким-то хуем не меняя DoorVisuals.BoltLights
public sealed class PointLightAirlockSystem : EntitySystem
{
    [Dependency] private readonly SharedPointLightSystem _pointLightSystem = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PointLightAirlockComponent, AppearanceChangeEvent>(OnLightsChanged);
    }

    private void ToggleLight(EntityUid uid, string hex, PointLightAirlockComponent airlockLight, bool enable = true)
    {
        if (!_pointLightSystem.TryGetLight(uid, out var pointLightComponent))
            return;

        if (enable)
        {
            var color = Color.FromHex(hex);
            _pointLightSystem.SetColor(uid, color, pointLightComponent);
        }

        _pointLightSystem.SetEnabled(uid, enable, pointLightComponent);

        RaiseLocalEvent(uid, new PointLightToggleEvent(enable), true);
        airlockLight.IsLightsEnabled = enable;

    }

    private void OnLightsChanged(EntityUid uid, PointLightAirlockComponent component, AppearanceChangeEvent args)
    {
        if (args.AppearanceData.TryGetValue(DoorVisuals.Powered, out var isPowered) && !(bool) isPowered)
        {
            if (component.IsLightsEnabled)
                ToggleLight(uid, string.Empty, component, false);

            return;
        }

        if (!args.AppearanceData.TryGetValue(DoorVisuals.State, out var state))
            return;

        if (!component.IsEmagged)
            HandleState(uid, component, (DoorState) state);

        if (args.AppearanceData.TryGetValue(DoorVisuals.EmergencyLights, out var emergency))
        {
            if ((bool) emergency)
                ToggleLight(uid, component.YellowColor, component, (bool) emergency);
            else if (!component.IsEmagged)
                HandleState(uid, component, (DoorState) state);
        }

        if (!args.AppearanceData.TryGetValue(DoorVisuals.BoltLights, out var boltsDown))
            return;

        if (component.LastBoltsState != (bool) boltsDown)
        {
            if ((bool) boltsDown)
                ToggleLight(uid, component.RedColor, component, (bool) boltsDown);
            else if (args.AppearanceData.TryGetValue(DoorVisuals.EmergencyLights, out var emergencyLights) && (bool) emergencyLights)
                ToggleLight(uid, component.YellowColor, component);
            else
            {
                if (component.IsEmagged)
                    component.IsEmagged = false;
                HandleState(uid, component, (DoorState) state);
            }
        }

        component.LastBoltsState = (bool) boltsDown;
    }

    private void HandleState(EntityUid uid, PointLightAirlockComponent component, DoorState state)
    {
        switch (state)
        {
            case DoorState.Emagging:
                ToggleLight(uid, component.RedColor, component);
                component.IsEmagged = true;
                break;

            case DoorState.Open:
                ToggleLight(uid, component.BlueColor, component);
                break;

            case DoorState.Opening:
                ToggleLight(uid, component.GreenColor, component);
                break;

            case DoorState.Closing:
                ToggleLight(uid, component.GreenColor, component);
                break;

            case DoorState.Closed:
                ToggleLight(uid, component.BlueColor, component);
                break;

            case DoorState.Denying:
                ToggleLight(uid, component.RedColor, component);
                break;
        }
    }

}
