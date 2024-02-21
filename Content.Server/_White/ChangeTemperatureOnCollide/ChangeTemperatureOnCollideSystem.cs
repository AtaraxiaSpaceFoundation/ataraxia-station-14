using Content.Server.Temperature.Components;
using Content.Server.Temperature.Systems;
using Robust.Shared.Physics.Events;

namespace Content.Server._White.ChangeTemperatureOnCollide;

public sealed class ChangeTemperatureOnCollideSystem : EntitySystem
{
    [Dependency] private readonly TemperatureSystem _temperature = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangeTemperatureOnCollideComponent, StartCollideEvent>(OnCollide);
    }

    private void OnCollide(EntityUid uid, ChangeTemperatureOnCollideComponent component, ref StartCollideEvent args)
    {
        if (args.OurFixtureId != component.FixtureID)
            return;

        if (!TryComp(args.OtherEntity, out TemperatureComponent? temperature))
            return;

        var curTemp = temperature.CurrentTemperature;
        var newTemp = curTemp + component.Temperature;

        if (curTemp < component.MinTemperature)
            newTemp = MathF.Max(curTemp, newTemp);
        else if (curTemp > component.MaxTemperature)
            newTemp = MathF.Min(curTemp, newTemp);
        else
            newTemp = Math.Clamp(newTemp, component.MinTemperature, component.MaxTemperature);

        _temperature.ForceChangeTemperature(args.OtherEntity, newTemp, temperature);
    }
}
