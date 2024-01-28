﻿using Robust.Shared.Network;

namespace Content.Server._White.Reputation;

[Serializable]
public sealed class UpdateCachedReputationEvent : EntityEventArgs
{
    public NetUserId Player;

    public UpdateCachedReputationEvent(NetUserId player)
    {
        Player = player;
    }
}
