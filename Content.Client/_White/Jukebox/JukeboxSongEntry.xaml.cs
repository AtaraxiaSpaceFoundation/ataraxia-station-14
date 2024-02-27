﻿using Content.Shared._White.Jukebox;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.XAML;

namespace Content.Client._White.Jukebox;

[GenerateTypedNameReferences]
public sealed partial class JukeboxSongEntry : Control
{
    public JukeboxSong? Song { get; private set; }

    private JukeboxSongEntry()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);
    }

    public JukeboxSongEntry(JukeboxSong song, Action<JukeboxSong> callback) : this()
    {
        Song = song;
        SongNameLabel.Text = Song.SongName;
        PlaySongButton.OnPressed += _ => callback.Invoke(Song);
    }
}