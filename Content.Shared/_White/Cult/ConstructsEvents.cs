﻿using Content.Shared.Actions;

namespace Content.Shared.White.Cult;

public sealed partial class ArtificerCreateSoulStoneActionEvent : InstantActionEvent
{
    public string SoulStonePrototypeId => "SoulShardGhost";
}

public sealed partial class ArtificerCreateConstructShellActionEvent : InstantActionEvent
{
    public string ShellPrototypeId => "ConstructShell";
}

public sealed partial class ArtificerConvertCultistFloorActionEvent : InstantActionEvent
{
    public string FloorTileId => "CultFloor";
}

public sealed partial class ArtificerCreateCultistWallActionEvent : InstantActionEvent
{
    public string WallPrototypeId => "WallCult";
}

public sealed partial class ArtificerCreateCultistAirlockActionEvent : InstantActionEvent
{
    public string AirlockPrototypeId => "AirlockGlassCult";
}

public sealed partial class WraithPhaseActionEvent : InstantActionEvent
{
    [DataField("duration")]
    public float Duration = 5f;

    public string StatusEffectId => "Incorporeal";
}

public sealed partial class JuggernautCreateWallActionEvent : InstantActionEvent
{
    public string WallPrototypeId = "WallInvisible";
}

