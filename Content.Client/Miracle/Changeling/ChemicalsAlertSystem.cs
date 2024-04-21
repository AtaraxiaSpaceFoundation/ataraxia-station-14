using Content.Client.Alerts;
using Content.Shared.Alert;
using Content.Shared.Changeling;
using Content.Shared.Revenant;

namespace Content.Client.Miracle.Changeling;

public sealed class ChemicalsAlertSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingComponent, UpdateAlertSpriteEvent>(OnUpdateAlert);
    }

    private void OnUpdateAlert(Entity<ChangelingComponent> ent, ref UpdateAlertSpriteEvent args)
    {
        if (args.Alert.AlertType != AlertType.Chemicals)
            return;

        var sprite = args.SpriteViewEnt.Comp;
        var chemicals = Math.Clamp(ent.Comp.ChemicalsBalance, 0, 999);
        sprite.LayerSetState(RevenantVisualLayers.Digit1, $"{(chemicals / 100) % 10}");
        sprite.LayerSetState(RevenantVisualLayers.Digit2, $"{(chemicals / 10) % 10}");
        sprite.LayerSetState(RevenantVisualLayers.Digit3, $"{chemicals % 10}");
    }
}
