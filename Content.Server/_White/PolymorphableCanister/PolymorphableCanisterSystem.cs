using Content.Server.Atmos.Piping.Unary.Components;
using Content.Shared._White.PolymorphableCanister;
using Content.Shared.Lock;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server._White.PolymorphableCanister;

public sealed class PolymorphableCanisterSystem : SharedPolymorphableCanisterSystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PolymorphableCanisterComponent, GetVerbsEvent<Verb>>(GetVerb);
    }

    private void GetVerb(EntityUid uid, PolymorphableCanisterComponent component, GetVerbsEvent<Verb> args)
    {
        if (TryComp(uid, out LockComponent? lockComponent) && lockComponent.Locked)
        {
            return;
        }

        if (TryComp(uid, out GasCanisterComponent? gasCanister) && gasCanister.Air.Pressure > 100)
        {
            return;
        }

        var changeAppearanceVerb = new Verb
        {
            Text = Loc.GetString("polymorphable-canister-change-appearance-verb"),
            Icon = new SpriteSpecifier.Rsi(new ResPath("Structures/Storage/canister.rsi"), "yellow"),
            Act = () => TryOpenUi(uid, args.User, component)
        };

        args.Verbs.Add(changeAppearanceVerb);
    }

    private void TryOpenUi(EntityUid uid, EntityUid user, PolymorphableCanisterComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!TryComp(user, out ActorComponent? actor))
            return;

        _ui.TryToggleUi(uid, PolymorphableCanisterUiKey.Key, actor.PlayerSession);
    }
}