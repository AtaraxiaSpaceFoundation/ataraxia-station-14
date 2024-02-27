using Content.Shared._White.Cult.Components;
using Content.Shared.Examine;
using Content.Shared.Ghost;

namespace Content.Shared._White.Cult.Systems;

public sealed class CultRuneSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultRuneComponent, ExamineAttemptEvent>(OnExamine);
    }

    private void OnExamine(Entity<CultRuneComponent> ent, ref ExamineAttemptEvent args)
    {
        if (HasComp<GhostComponent>(args.Examiner) || HasComp<CultistComponent>(args.Examiner))
            return;

        args.Cancel();
    }
}
