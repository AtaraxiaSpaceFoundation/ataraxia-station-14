using System.Linq;
using Content.Shared.Standing;
using Content.Shared.Standing.Systems;
using Content.Shared.StatusEffect;
using Robust.Shared.Map;

namespace Content.Shared._White.BetrayalDagger;

public sealed class TelefragSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedStandingStateSystem _standingState = default!;

    public void Telefrag(EntityCoordinates coords, EntityUid user, float range = 0.2f)
    {
        var ents = new HashSet<Entity<StandingStateComponent>>();
        _lookup.GetEntitiesInRange(coords, range, ents);

        foreach (var ent in ents.Where(ent => ent.Owner != user))
        {
            _standingState.TryLieDown(ent, ent, true);
        }
    }
}
