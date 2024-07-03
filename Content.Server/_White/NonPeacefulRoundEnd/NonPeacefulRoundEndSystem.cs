using System.Linq;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared._White;
using Content.Shared._White.NonPeacefulRoundEnd;
using Robust.Server.Audio;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._White.NonPeacefulRoundEnd;

public sealed class NonPeacefulRoundEndSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly AudioSystem _sharedAudioSystem = default!;

    private NonPeacefulRoundItemsPrototype _nonPeacefulRoundItemsPrototype = default!;

    private bool _enabled;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEnded);
        _cfg.OnValueChanged(WhiteCVars.NonPeacefulRoundEndEnabled, value => _enabled = value, true);
    }

    private void OnRoundEnded(RoundEndTextAppendEvent ev)
    {
        if (!_enabled)
            return;

        var prototypes =  _prototypeManager.EnumeratePrototypes<NonPeacefulRoundItemsPrototype>().ToList();

        if (prototypes.Count < 1)
        {
            return;
        }

        _nonPeacefulRoundItemsPrototype = _robustRandom.Pick(prototypes);

        foreach (var session in _playerManager.Sessions)
        {
            if (!session.AttachedEntity.HasValue) continue;

            RemComp<PacifiedComponent>(session.AttachedEntity.Value);

            GiveItem(session.AttachedEntity.Value);
        }

        var announceCount = _robustRandom.Next(5,15);

        for (int i = 0; i <= announceCount; i++)
        {
            _chatManager.SendAdminAnnouncement("!!!РЕЗНЯ!!!");
        }

        _sharedAudioSystem.PlayGlobal("/Audio/White/RoundEnd/rezniya.ogg", Filter.Broadcast(), false);
    }

    private void GiveItem(EntityUid player)
    {
        var item = _robustRandom.Pick(_nonPeacefulRoundItemsPrototype.Items);

        var transform = CompOrNull<TransformComponent>(player);

        if(transform == null)
            return;

        if(!HasComp<HumanoidAppearanceComponent>(player))

            return;
        if(!HasComp<HandsComponent>(player))
            return;


        var weaponEntity = _entityManager.SpawnEntity(item, transform.Coordinates);

        // _handsSystem.TryDrop(player);
        _handsSystem.PickupOrDrop(player, weaponEntity);
    }
}
