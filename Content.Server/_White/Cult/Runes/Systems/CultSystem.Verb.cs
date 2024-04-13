using System.Linq;
using Content.Server.Body.Components;
using Content.Shared._White.Cult;
using Content.Shared._White.Cult.Components;
using Content.Shared._White.Cult.UI;
using Content.Shared.DoAfter;
using Content.Shared.Verbs;
using Robust.Shared.Player;

namespace Content.Server._White.Cult.Runes.Systems;

public sealed partial class CultSystem
{
    public void InitializeVerb()
    {
        SubscribeLocalEvent<CultistComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
        SubscribeLocalEvent<CultistComponent, CultEmpowerSelectedBuiMessage>(OnCultistEmpowerSelected);
        SubscribeLocalEvent<CultistComponent, CultEmpowerRemoveBuiMessage>(OnCultistEmpowerRemove);
        SubscribeLocalEvent<CultistComponent, SpellCreatedEvent>(OnSpellCreated);
        SubscribeLocalEvent<CultistComponent, CultistFactoryItemSelectedMessage>(OnBloodRitesSelected);
    }

    private void OnCultistEmpowerRemove(Entity<CultistComponent> ent, ref CultEmpowerRemoveBuiMessage args)
    {
        var entity = GetEntity(args.ActionType);
        ent.Comp.SelectedEmpowers.Remove(args.ActionType);
        _actionsSystem.RemoveAction(entity);
        Dirty(ent);
    }

    private void OnSpellCreated(EntityUid ent, CultistComponent comp, SpellCreatedEvent args)
    {
        if (args.Cancelled || comp.SelectedEmpowers.Count >= 2)
            return;

        var action = CultistComponent.CultistActions.FirstOrDefault(x => x.Equals(args.Spell));

        if (action == null)
            return;

        var howMuchBloodTake = HasComp<CultBuffComponent>(ent) ? -10f : -20f;

        if (!TryComp<BloodstreamComponent>(ent, out var bloodstreamComponent))
            return;

        _bloodstreamSystem.TryModifyBloodLevel(ent, howMuchBloodTake, bloodstreamComponent, createPuddle: false);

        comp.SelectedEmpowers.Add(GetNetEntity(_actionsSystem.AddAction(ent, args.Spell)));

        Dirty(ent, comp);
    }

    private void OnCultistEmpowerSelected(EntityUid ent, CultistComponent comp, CultEmpowerSelectedBuiMessage args)
    {
        var action = CultistComponent.CultistActions.FirstOrDefault(x => x.Equals(args.ActionType));

        if (action == null)
            return;

        if (comp.SelectedEmpowers.Count >= 2)
        {
            _popupSystem.PopupEntity(Loc.GetString("verb-spell-create-too-much"), ent, ent);
            return;
        }

        var creationTime = HasComp<CultBuffComponent>(ent) ? 2.5f : 5f;

        _doAfterSystem.TryStartDoAfter(
            new DoAfterArgs(_entityManager, ent, creationTime, new SpellCreatedEvent {Spell = action}, ent)
            {
                BreakOnDamage = true,
                BreakOnMove = true
            });
    }

    private void OnGetVerbs(Entity<CultistComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (ent.Owner != args.User || !TryComp<ActorComponent>(args.User, out var actor))
            return;

        var createSpellVerb = new Verb
        {
            Text = Loc.GetString("verb-spell-create-text"),
            Message = Loc.GetString("verb-spell-create-message"),
            Category = VerbCategory.Cult,
            Act = () =>
            {
                _ui.TryOpen(ent, CultEmpowerUiKey.Key, actor.PlayerSession);
            }
        };

        var removeSpellVerb = new Verb
        {
            Text = Loc.GetString("verb-spell-remove-text"),
            Message = Loc.GetString("verb-spell-remove-message"),
            Category = VerbCategory.Cult,
            Act = () =>
            {
                RemoveSpell(ent, actor.PlayerSession);
            }
        };

        var bloodRitesVerb = new Verb
        {
            Text = Loc.GetString("verb-blood-rites-text"),
            Message = Loc.GetString("verb-blood-rites-message", ("blood", ent.Comp.RitesBloodAmount)),
            Category = VerbCategory.Cult,
            Act = () =>
            {
                if (!_ui.TryGetUi(ent, BloodRitesUi.Key, out var bui))
                    return;

                _ui.SetUiState(bui, new CultistFactoryBUIState(ent.Comp.BloodRites));
                _ui.OpenUi(bui, actor.PlayerSession);
            }
        };

        args.Verbs.Add(createSpellVerb);
        args.Verbs.Add(removeSpellVerb);
        args.Verbs.Add(bloodRitesVerb);
    }

    private void RemoveSpell(Entity<CultistComponent> ent, ICommonSession session)
    {
        if (ent.Comp.SelectedEmpowers.Count == 0)
        {
            _popupSystem.PopupEntity(Loc.GetString("verb-spell-remove-no-spells"), ent, ent);
            return;
        }

        _ui.TryOpen(ent, CultEmpowerRemoveUiKey.Key, session);
    }

    private void OnBloodRitesSelected(Entity<CultistComponent> ent, ref CultistFactoryItemSelectedMessage args)
    {
        if (!_prototypeManager.TryIndex<CultistFactoryProductionPrototype>(args.Item, out var prototype))
            return;

        if (ent.Comp.RitesBloodAmount < prototype.BloodCost)
        {
            var message = Loc.GetString("verb-blood-rites-no-blood", ("required", prototype.BloodCost),
                ("blood", ent.Comp.RitesBloodAmount));
            _popupSystem.PopupEntity(message, ent, ent);
            return;
        }

        var success = false;
        foreach (var item in prototype.Item)
        {
            var entity = Spawn(item, Transform(ent).Coordinates);
            if (_handsSystem.TryPickupAnyHand(ent, entity))
            {
                success = true;
                continue;
            }

            _popupSystem.PopupEntity(Loc.GetString("verb-blood-rites-no-hand"), ent, ent);
            QueueDel(entity);
            break;
        }

        if (success)
            ent.Comp.RitesBloodAmount -= prototype.BloodCost;
    }
}
