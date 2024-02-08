using Content.Server.Access.Systems;
using Content.Server.Humanoid;
using Content.Server.IdentityManagement;
using Content.Server.PDA;
using Content.Server.Roles;
using Content.Server.StationRecords.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Mind.Components;
using Content.Shared.NukeOps;
using Content.Shared.PDA;
using Content.Shared.Preferences;
using Content.Shared.StationRecords;

namespace Content.Server._White.Other.RandomHumanSystem;

public sealed class RandomHumanSystem : EntitySystem
{
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly IdCardSystem _card = default!;
    [Dependency] private readonly PdaSystem _pda = default!;
    [Dependency] private readonly IdentitySystem _identity = default!;
    [Dependency] private readonly StationRecordsSystem _records = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomHumanComponent, ComponentInit>(OnInit);
    }

    private void OnInit(EntityUid uid, RandomHumanComponent component, ComponentInit args)
    {
        if (!TryComp(uid, out HumanoidAppearanceComponent? humanoidAppearanceComponent))
        {
            return;
        }

        var newProfile = HumanoidCharacterProfile.RandomWithSpecies();

        _humanoid.LoadProfile(uid, newProfile, humanoidAppearanceComponent);

        if (HasComp<NukeOperativeComponent>(uid))
            return;

        _metaData.SetEntityName(uid, newProfile.Name);

        if (!_inventorySystem.TryGetSlotEntity(uid, "id", out var idUid))
            return;

        if (!EntityManager.TryGetComponent(idUid, out PdaComponent? pdaComponent) ||
            !TryComp<IdCardComponent>(pdaComponent.ContainedId, out var card))
            return;

        var cardId = pdaComponent.ContainedId.Value;

        _card.TryChangeFullName(cardId, newProfile.Name, card);
        _pda.SetOwner(idUid.Value, pdaComponent, newProfile.Name);

        if (EntityManager.TryGetComponent(cardId, out StationRecordKeyStorageComponent? keyStorage)
            && keyStorage.Key is { } key)
        {
            if (_records.TryGetRecord<GeneralStationRecord>(key, out var generalRecord))
            {
                generalRecord.Name = newProfile.Name;
                generalRecord.Age = newProfile.Age;
                generalRecord.Gender = newProfile.Gender;
                generalRecord.Species = newProfile.Species;
                generalRecord.Profile = newProfile;
            }

            _records.Synchronize(key);
        }

        _identity.QueueIdentityUpdate(uid);
    }
}
