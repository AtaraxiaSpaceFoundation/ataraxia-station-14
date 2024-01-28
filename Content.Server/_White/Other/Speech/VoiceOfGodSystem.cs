using Content.Server.Speech;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server._White.Other.Speech;

public sealed class VoiceOfGodSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VoiceOfGodComponent, AccentGetEvent>(OnAccent);
    }

    private string Accentuate(VoiceOfGodComponent component, string message)
    {
        if (!string.IsNullOrEmpty(component.Sound))
        {
            _audio.PlayPvs(component.Sound,
                component.Owner,
                new AudioParams()
                {
                    Volume = component.Volume
                }
            );
        }

        return component.Accent ? message.ToUpper() : message;
    }

    private void OnAccent(EntityUid uid, VoiceOfGodComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(component, args.Message);
    }
}
