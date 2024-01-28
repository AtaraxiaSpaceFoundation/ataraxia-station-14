using Content.Shared._White.Jukebox;

namespace Content.Client._White.Jukebox;

public sealed class ClientJukeboxSongsSyncManager : JukeboxSongsSyncManager
{
    public override void OnSongUploaded(JukeboxSongUploadNetMessage message)
    {
        ContentRoot.AddOrUpdateFile(message.RelativePath!, message.Data);
    }
}
