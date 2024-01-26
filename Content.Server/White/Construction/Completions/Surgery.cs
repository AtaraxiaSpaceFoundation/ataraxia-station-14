using Content.Server.Body.Systems;
using Content.Shared.Body.Organ;
using Content.Shared.Construction;
using Content.Shared.White.CheapSurgery;

namespace Content.Server.White.Construction.Completions;

public sealed partial class Surgery : IGraphAction
{
    private ISawmill _sawmill = default!;

    public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
    {
        _sawmill = Logger.GetSawmill("Surgery");
        var bodySystem = entityManager.EntitySysManager.GetEntitySystem<BodySystem>();

        if (!entityManager.TryGetComponent<ActiveSurgeryComponent>(uid, out var surgeryComponent))
        {
            _sawmill.Warning($"Entity {uid} does not have a ActiveSurgery Component");
            return;
        }

        if (entityManager.TryGetComponent<OrganComponent>(surgeryComponent.OrganUid, out var organComponent))
            bodySystem.RemoveOrgan(surgeryComponent.OrganUid, organComponent);

        entityManager.RemoveComponent<ActiveSurgeryComponent>(uid);
    }
}
