﻿namespace Content.Shared.Chat;

public static class ChatChannelExtensions
{
    public static Color TextColor(this ChatChannel channel)
    {
        return channel switch
        {
            ChatChannel.Server => Color.FromHex("#9051a8"),
            ChatChannel.Radio => Color.LimeGreen,
            ChatChannel.LOOC => Color.MediumTurquoise,
            ChatChannel.OOC => Color.LightSkyBlue,
            ChatChannel.Dead => Color.MediumPurple,
            ChatChannel.Admin => Color.Red,
            ChatChannel.AdminAlert => Color.Red,
            ChatChannel.AdminChat => Color.HotPink,
            ChatChannel.Whisper => Color.DarkGray,
            ChatChannel.Changeling => Color.Purple,
            ChatChannel.Cult => Color.DarkRed, // WD EDIT
            ChatChannel.XenoHivemind => Color.FromHex("#600a91"),
            _ => Color.LightGray
        };
    }
}
