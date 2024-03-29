using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Robust.Shared.Player;

namespace Content.Server.Players.PlayTimeTracking;

public interface IPlayTimeTrackingManager
{
    event CalcPlayTimeTrackersCallback? CalcTrackers;

    void Initialize();

    void Shutdown();

    void Update();

    void FlushAllTrackers();

    void FlushTracker(ICommonSession player);

    void SaveSession(ICommonSession session);

    public Task LoadData(ICommonSession session, CancellationToken cancel);

    void ClientDisconnected(ICommonSession session);
    void AddTimeToOverallPlaytime(ICommonSession id, TimeSpan time);

    TimeSpan GetOverallPlaytime(ICommonSession id);

    bool TryGetTrackerTimes(ICommonSession id, [NotNullWhen(true)] out Dictionary<string, TimeSpan>? time);

    Dictionary<string, TimeSpan> GetTrackerTimes(ICommonSession id);
    TimeSpan GetPlayTimeForTracker(ICommonSession id, string tracker);
    void AddTimeToTracker(ICommonSession id, string tracker, TimeSpan time);
    public void QueueRefreshTrackers(ICommonSession player);
    public void QueueSendTimers(ICommonSession player);
    void Save();
}
