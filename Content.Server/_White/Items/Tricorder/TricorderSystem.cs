using Content.Server.Medical.Components;
using Content.Server.UserInterface;
using Content.Shared.Atmos.Components;
using Content.Shared.Database;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.Examine;
using Content.Shared.MedicalScanner;
using Content.Shared.Verbs;
using Content.Shared._White.Item.Tricorder;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Utility;

namespace Content.Server._White.Items.Tricorder;

public sealed class TricorderSystem : SharedTricorderSystem
{
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly AudioSystem _audioSystem = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TricorderComponent, ExaminedEvent>(OnExamined);

        SubscribeLocalEvent<TricorderComponent, GetVerbsEvent<AlternativeVerb>>(OnAddSwitchModeVerbs);
    }

    private void OnExamined(EntityUid uid, TricorderComponent component, ExaminedEvent args)
    {
        var mode = GetNameByMode(component.CurrentMode);
        args.PushMarkup(Loc.GetString("network-configurator-examine-current-mode", ("mode", Loc.GetString(mode))));
    }

    private void OnAddSwitchModeVerbs(EntityUid uid, TricorderComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.Using.HasValue || !HasComp<TricorderComponent>(args.Target))
        {
            return;
        }

        var icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/settings.svg.192dpi.png"));

        AlternativeVerb switchToMultitoolVerb = new()
        {
            Text = "Переключить на мультитул",
            Icon = icon,
            Act = () => SwitchToMode(args.User, uid, component, TricorderMode.Multitool),
            Impact = LogImpact.Low
        };

        AlternativeVerb switchToGasAnalyzerlVerb = new()
        {
            Text = "Переключить на газоанализатор",
            Icon = icon,
            Act = () => SwitchToMode(args.User, uid, component, TricorderMode.GasAnalyzer),
            Impact = LogImpact.Low
        };

        AlternativeVerb switchToHealthAnalyzerVerb = new()
        {
            Text = "Переключить на анализатор здоровья",
            Icon = icon,
            Act = () => SwitchToMode(args.User, uid, component, TricorderMode.HealthAnalyzer),
            Impact = LogImpact.Low
        };

        args.Verbs.Add(switchToHealthAnalyzerVerb);
        args.Verbs.Add(switchToGasAnalyzerlVerb);
        args.Verbs.Add(switchToMultitoolVerb);
    }

    public void SwitchToMode(EntityUid? user, EntityUid tricoderUid, TricorderComponent tricorder, TricorderMode mode)
    {
        if (tricorder.CurrentMode == mode)
        {
            return;
        }

        tricorder.CurrentMode = mode;

        switch (mode)
        {
            case TricorderMode.Multitool:
                SetToMultitool(tricoderUid);
                break;
            case TricorderMode.GasAnalyzer:
                SetToGasAnalyzer(tricoderUid);
                break;
            case TricorderMode.HealthAnalyzer:
                SetToHealthAnalyzer(tricoderUid);
                break;
        }

        if (!user.HasValue)
            return;

        UpdateModeAppearance(user.Value, tricoderUid, tricorder);
    }

    private void UpdateModeAppearance(EntityUid userUid, EntityUid tricoderUid, TricorderComponent tricorder)
    {
        _audioSystem.PlayPvs(tricorder.SoundSwitchMode, userUid, AudioParams.Default.WithVolume(1.5f));
        Dirty(tricoderUid, tricorder);
    }

    private void SetToMultitool(EntityUid uid)
    {
        RemComp<GasAnalyzerComponent>(uid);
        RemComp<HealthAnalyzerComponent>(uid);

        var comp = AddComp<NetworkConfiguratorComponent>(uid);
        if (TryComp(uid, out ActivatableUIComponent? ui))
        {
            ui.Key = NetworkConfiguratorUiKey.Configure;
        }

        Dirty(uid, comp);
    }

    private void SetToGasAnalyzer(EntityUid uid)
    {
        RemComp<NetworkConfiguratorComponent>(uid);
        RemComp<HealthAnalyzerComponent>(uid);

        var comp = AddComp<GasAnalyzerComponent>(uid);
        if (TryComp(uid, out ActivatableUIComponent? ui))
        {
            ui.Key = GasAnalyzerComponent.GasAnalyzerUiKey.Key;
        }

        Dirty(uid, comp);
    }

    private void SetToHealthAnalyzer(EntityUid uid)
    {
        RemComp<NetworkConfiguratorComponent>(uid);
        RemComp<GasAnalyzerComponent>(uid);

        var healthAnalyzerComponent = _componentFactory.GetComponent<HealthAnalyzerComponent>();
        healthAnalyzerComponent.ScanningEndSound = new SoundPathSpecifier("/Audio/Items/Medical/healthscanner.ogg");
        healthAnalyzerComponent.Owner = uid;

        _entityManager.AddComponent(uid, healthAnalyzerComponent);
        if (TryComp(uid, out ActivatableUIComponent? ui))
        {
            ui.Key = HealthAnalyzerUiKey.Key;
        }
    }
}
