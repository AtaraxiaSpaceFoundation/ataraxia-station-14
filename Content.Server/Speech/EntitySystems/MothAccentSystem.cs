using System.Text.RegularExpressions;
using Content.Server.Speech.Components;

namespace Content.Server.Speech.EntitySystems;

public sealed class MothAccentSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MothAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, MothAccentComponent component, AccentGetEvent args)
    {
        var message = args.Message;

        // buzzz
        message = Regex.Replace(message, "z{1,3}", "zzz");
        // buZZZ
        message = Regex.Replace(message, "Z{1,3}", "ZZZ");

        // WD EDIT START
        message = Regex.Replace(message, "з{1,3}", "ззз");

        message = Regex.Replace(message, "З{1,3}", "ЗЗЗ");

        message = Regex.Replace(message, "ж{1,3}", "жжж");

        message = Regex.Replace(message, "Ж{1,3}", "ЖЖЖ");
        // WD EDIT END

        args.Message = message;
    }
}
