using Content.Server.Bed.Sleep;
using Content.Server.Popups;
using Content.Server.Revenant.Components;
using Content.Shared.Bed.Sleep;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Random;

namespace Content.Server.Revenant.EntitySystems;

public sealed class BlightSystem : EntitySystem
{
    [Dependency] private readonly SleepingSystem _sleeping = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlightComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<BlightComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<BlightComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnShutdown(Entity<BlightComponent> ent, ref ComponentShutdown args)
    {
        if (!Deleted(ent) && !EntityManager.IsQueuedForDeletion(ent) && _mobState.IsAlive(ent))
            _popup.PopupEntity("Вы вновь чувствуете себя здоровым.", ent, ent);
    }

    private void OnStartup(Entity<BlightComponent> ent, ref ComponentStartup args)
    {
        SetDelay(ent.Comp);
        SetDuration(ent.Comp);
    }

    private void OnMobStateChanged(Entity<BlightComponent> ent, ref MobStateChangedEvent args)
    {
        RemCompDeferred<BlightComponent>(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<BlightComponent>();
        var sleepingQuery = GetEntityQuery<SleepingComponent>();

        while (query.MoveNext(out var ent, out var blight))
        {
            blight.Duration += frameTime;

            if (blight.Duration >= blight.MaxDuration.TotalSeconds)
            {
                RemCompDeferred<BlightComponent>(ent);
                continue;
            }

            if (sleepingQuery.HasComponent(ent))
            {
                if (blight.BedSleep)
                {
                    blight.SleepDelay += frameTime;
                    if (blight.SleepDelay >= blight.SleepingCureTime.TotalSeconds)
                        RemCompDeferred<BlightComponent>(ent);
                }

                continue;
            }

            blight.BedSleep = false;
            blight.SleepDelay = 0f;

            blight.Delay += frameTime;

            if (blight.Delay < blight.MaxDelay.TotalSeconds)
                continue;

            _sleeping.TrySleeping(ent);

            blight.Delay = 0f;
            SetDelay(blight);
        }
    }

    private void SetDuration(BlightComponent comp)
    {
        comp.MaxDuration = TimeSpan.FromSeconds(_random.Next(300, 420));
    }

    private void SetDelay(BlightComponent comp)
    {
        comp.MaxDelay = TimeSpan.FromSeconds(_random.Next(10, 30));
    }
}
