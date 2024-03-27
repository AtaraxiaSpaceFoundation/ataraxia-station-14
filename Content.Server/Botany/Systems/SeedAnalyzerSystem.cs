using Content.Server.Botany.Components;
using Content.Server.PowerCell;
using Content.Shared.Botany;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Server.Botany.Systems;

public sealed class SeedAnalyzerSystem : EntitySystem
{
    [Dependency] private readonly PowerCellSystem _cell = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SeedAnalyzerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<SeedAnalyzerComponent, SeedAnalyzerDoAfterEvent>(OnDoAfter);
    }

    private void OnAfterInteract(EntityUid uid, SeedAnalyzerComponent seedAnalyzer, AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach || !HasComp<PlantHolderComponent>(args.Target) ||
            !_cell.HasActivatableCharge(uid, user: args.User))
        {
            return;
        }

        _audio.PlayPvs(seedAnalyzer.ScanningBeginSound, uid);

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, seedAnalyzer.ScanDelay,
            new SeedAnalyzerDoAfterEvent(), uid, target: args.Target, used: uid)
        {
            BreakOnMove = true,
            NeedHand = true
        });
    }

    private void OnDoAfter(EntityUid uid, SeedAnalyzerComponent component, DoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target == null ||
            !_cell.TryUseActivatableCharge(uid, user: args.User))
            return;

        _audio.PlayPvs(component.ScanningEndSound, args.Args.User);

        UpdateScannedSeed(uid, args.Args.User, args.Args.Target.Value, component);
        args.Handled = true;
    }

    private void OpenUserInterface(EntityUid user, EntityUid seedAnalyzer)
    {
        if (!TryComp<ActorComponent>(user, out var actor) ||
            !_uiSystem.TryGetUi(seedAnalyzer, SeedAnalyzerUiKey.Key, out var ui))
            return;

        _uiSystem.OpenUi(ui, actor.PlayerSession);
    }

    public void UpdateScannedSeed(
        EntityUid uid,
        EntityUid user,
        EntityUid? target,
        SeedAnalyzerComponent? seedAnalyzer = null)
    {
        if (!Resolve(uid, ref seedAnalyzer))
            return;

        if (target == null || !_uiSystem.TryGetUi(uid, SeedAnalyzerUiKey.Key, out var ui))
            return;

        if (!TryComp<PlantHolderComponent>(target, out var plant))
            return;

        if (plant.Seed == null)
        {
            return;
        }

        var zero2 = FixedPoint2.Zero;
        var emptydic = new Dictionary<string, FixedPoint2> { { "No chemicals", zero2 } };
        var passchems = new Dictionary<string, FixedPoint2>();

        if (plant.Seed?.Chemicals != null)
        {
            foreach (var (chem, quantity) in plant.Seed.Chemicals)
            {
                var amount = FixedPoint2.New(quantity.Min);
                amount += FixedPoint2.New(plant.Seed.Potency / quantity.PotencyDivisor);
                amount = FixedPoint2.New((int) MathHelper.Clamp(amount.Float(), quantity.Min, quantity.Max));
                passchems?.Add(chem, amount);
            }
        }

        OpenUserInterface(user, uid);

        _uiSystem.SendUiMessage(ui, new SeedAnalyzerScannedUserMessage(GetNetEntity(target),
            plant.Seed?.Yield,
            plant.Seed?.Production,
            plant.Seed?.Lifespan,
            plant.Seed?.Maturation,
            plant.Seed?.Endurance,
            plant.Seed?.Potency,
            plant.Seed?.Viable,
            plant.Seed?.TurnIntoKudzu,
            plant.Seed?.Seedless,
            plant.Seed?.DisplayName,
            passchems ?? emptydic,
            plant.Seed?.Ligneous,
            plant.Seed?.CanScream,
            plant.Seed?.Slip,
            plant.Seed?.Bioluminescent,
            plant.Seed?.Sentient));
    }
}