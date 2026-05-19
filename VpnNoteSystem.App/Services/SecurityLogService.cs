using Microsoft.EntityFrameworkCore;
using VpnNoteSystem.App.Data;
using VpnNoteSystem.App.Models;

namespace VpnNoteSystem.App.Services;

public class SecurityLogService : ISecurityLogService
{
    private readonly AppDbContext _context;

    public SecurityLogService(AppDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(int? userId, string username, string action,
        string details, bool isSuccess)
    {
        var log = new SecurityLog
        {
            UserId = userId,
            Username = username,
            Action = action,
            Details = details,
            IpAddress = "127.0.0.1",
            IsSuccess = isSuccess,
            Timestamp = DateTime.UtcNow
        };

        _context.SecurityLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task<List<SecurityLog>> GetRecentLogsAsync(int count = 20)
    {
        return await _context.SecurityLogs
            .OrderByDescending(l => l.Timestamp)
            .Take(count)
            .ToListAsync();
    }
}
