﻿using System.Linq;
using Content.Server.Cargo.Components;
using Content.Server.Cargo.Systems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.StationEvents.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.StationEvents.Events;

public sealed class CargoGiftsRule : StationEventSystem<CargoGiftsRuleComponent>
{
    [Dependency] private readonly CargoSystem _cargoSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly RampingStationEventSchedulerSystem _rampingEventSystem = default!; // WD

    protected override void Added(EntityUid uid, CargoGiftsRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        if (_rampingEventSystem.CheckRampingEventRule()) // WD START
        {
            ForceEndSelf(uid, gameRule);
            return;
        } // WD END

        base.Added(uid, component, gameRule, args);

        var str = Loc.GetString(component.Announce,
            ("sender", Loc.GetString(component.Sender)), ("description", Loc.GetString(component.Description)), ("dest", Loc.GetString(component.Dest)));
        ChatSystem.DispatchGlobalAnnouncement(str, colorOverride: Color.FromHex("#18abf5"));
    }

    /// <summary>
    /// Called on an active gamerule entity in the Update function
    /// </summary>
    protected override void ActiveTick(EntityUid uid, CargoGiftsRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        if (component.Gifts.Count == 0)
            return;

        if (component.TimeUntilNextGifts > 0)
        {
            component.TimeUntilNextGifts -= frameTime;
            return;
        }

        component.TimeUntilNextGifts += 30f;

        if (!TryGetRandomStation(out var station, HasComp<StationCargoOrderDatabaseComponent>))
            return;

        if (!TryComp<StationCargoOrderDatabaseComponent>(station, out var cargoDb))
        {
            return;
        }

        // Add some presents
        var outstanding = CargoSystem.GetOutstandingOrderCount(cargoDb);
        while (outstanding < cargoDb.Capacity - component.OrderSpaceToLeave && component.Gifts.Count > 0)
        {
            // I wish there was a nice way to pop this
            var (productId, qty) = component.Gifts.First();
            component.Gifts.Remove(productId);

            var product = _prototypeManager.Index(productId);

            if (!_cargoSystem.AddAndApproveOrder(
                    station!.Value,
                    product.Product,
                    product.Cost,
                    qty,
                    Loc.GetString(component.Sender),
                    Loc.GetString(component.Description),
                    Loc.GetString(component.Dest),
                    cargoDb
            ))
            {
                break;
            }
        }

        if (component.Gifts.Count == 0)
        {
            // We're done here!
            _ticker.EndGameRule(uid, gameRule);
        }
    }

}
