using System.Linq;
using Robust.Shared.Random;

namespace Content.Shared.Changeling;

public sealed class ChangelingNameGenerator
{
    [Dependency] private readonly IRobustRandom _random = default!;

    private List<string> _used = new();

    private readonly List<string> _greekAlphabet = new()
    {
        "Alpha",
        "Beta",
        "Gamma",
        "Delta",
        "Epsilon",
        "Zeta",
        "Eta",
        "Theta",
        "Iota",
        "Kappa",
        "Lambda",
        "Mu",
        "Nu",
        "Xi",
        "Omicron",
        "Pi",
        "Rho",
        "Sigma",
        "Tau",
        "Upsilon",
        "Phi",
        "Chi",
        "Psi",
        "Omega"
    };

    private string GenWhiteLabelName()
    {
        var number = _random.Next(0,10000);
        return $"HiveMember-{number}";
    }

    public string GetName()
    {
        _random.Shuffle(_greekAlphabet);

        foreach (var selected in _greekAlphabet.Where(selected => !_used.Contains(selected)))
        {
            _used.Add(selected);
            return selected;
        }

        return GenWhiteLabelName();
    }

    public void ClearUsed()
    {
        _used.Clear();
    }
}
