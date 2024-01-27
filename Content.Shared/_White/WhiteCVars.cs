using Robust.Shared.Configuration;

namespace Content.Shared._White;

[CVarDefs]
public class WhiteCVars
{
    public static readonly CVarDef<int> CultMinPlayers =
        CVarDef.Create("white.cult_min_players", 20, CVar.SERVERONLY | CVar.ARCHIVE);

    public static readonly CVarDef<int> CultMaxStartingPlayers =
        CVarDef.Create("white.cult_max_starting_players", 4, CVar.SERVERONLY | CVar.ARCHIVE);

    public static readonly CVarDef<int> CultMinStartingPlayers =
        CVarDef.Create("white.cult_min_starting_players", 2, CVar.SERVERONLY | CVar.ARCHIVE);
}
