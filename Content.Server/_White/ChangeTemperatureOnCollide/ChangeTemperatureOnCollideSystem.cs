using Content.Server.Temperature.Components;
using Content.Server.Temperature.Systems;
using Content.Shared.Atmos;
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

        _temperature.ForceChangeTemperature(args.OtherEntity,
            MathF.Max(Atmospherics.TCMB, temperature.CurrentTemperature + component.Temperature), temperature);
    }
}
