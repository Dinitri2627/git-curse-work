using VpnNoteSystem.App.Models;

namespace VpnNoteSystem.App.Services;

public interface ISecurityLogService
{
    Task LogAsync(int? userId, string username, string action, string details, bool isSuccess);
    Task<List<SecurityLog>> GetRecentLogsAsync(int count = 20);
}
