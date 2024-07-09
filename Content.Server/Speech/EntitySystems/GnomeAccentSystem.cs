using Content.Server.Speech.Components;
using System.Text.RegularExpressions;

namespace Content.Server.Speech.EntitySystems;

/// <summary>
/// System that Gnomes the Gnomes talking
/// </summary>
public sealed class GnomeAccentSystem : EntitySystem
{
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GnomeAccentComponent, AccentGetEvent>(OnAccentGet);
    }
    public string Accentuate(string message, GnomeAccentComponent component)
    {
        var msg = message;

        msg = _replacement.ApplyReplacements(msg, "gnome");

        // Пиздец, а не код

        msg = Regex.Replace(msg, @"(?<!\w)\bне", "ГНЕМ", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bнет", "ГНЕМТ", RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bнахуй", "ГНАМХУЙ", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bпидоры", "ГНОМЕРЫ", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bхуесос", "ГНОХУСОМ", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bебал", "ГНОМИЛ", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bзаебал", "ЗАГНОМИЛ", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bубил", "УГНОМИЛ", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bубит", "УГНОМЛЕН", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bебнул", "УГНОМЛЕН", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bстрелял", "СТРЕГНОМИЛ", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bзаколол", "СГНОМИЛ", RegexOptions.IgnoreCase);

        msg = Regex.Replace(msg, @"(?<!\w)\bмой", "муй", RegexOptions.None);
        msg = Regex.Replace(msg, @"(?<!\w)\bдруг", "бро", RegexOptions.None);
        msg = Regex.Replace(msg, @"(?<!\w)\bдрузья", "друганы", RegexOptions.None);
        return msg;
    }


    private void OnAccentGet(EntityUid uid, GnomeAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message, component);
    }
}
