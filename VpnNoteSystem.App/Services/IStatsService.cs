using VpnNoteSystem.App.Models;

namespace VpnNoteSystem.App.Services;

public interface IStatsService
{
    Task<SystemStats> CollectCurrentStatsAsync();
    Task<List<SystemStats>> GetStatsHistoryAsync(string deviceName, int count = 10);
}
