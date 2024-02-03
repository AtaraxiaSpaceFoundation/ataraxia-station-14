using Content.Shared._Miracle.GulagSystem;
using Robust.Shared.Network;

namespace Content.Server._Miracle.Components;

[RegisterComponent]
[Access(typeof(SharedGulagSystem))]

public sealed partial class GulagOreProcessorComponent : Component
{
    //I hate my life
    public NetUserId? LastInteractedUser;
}

