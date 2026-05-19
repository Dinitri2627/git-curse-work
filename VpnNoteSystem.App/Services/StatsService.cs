using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.EntityFrameworkCore;
using VpnNoteSystem.App.Data;
using VpnNoteSystem.App.Models;

namespace VpnNoteSystem.App.Services;

public class StatsService : IStatsService
{
    private readonly AppDbContext _context;
    private readonly IAuthService _authService;
    private readonly ISecurityLogService _securityLog;

    public StatsService(AppDbContext context, IAuthService authService,
        ISecurityLogService securityLog)
    {
        _context = context;
        _authService = authService;
        _securityLog = securityLog;
    }

    public async Task<SystemStats> CollectCurrentStatsAsync()
    {
        var user = _authService.GetCurrentUser()
            ?? throw new UnauthorizedAccessException("Необходима аутентификация");

        var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        cpuCounter.NextValue();
        await Task.Delay(500);
        var cpuUsage = Math.Round(cpuCounter.NextValue(), 2);

        var ramAvailable = new PerformanceCounter("Memory", "Available MBytes");
        var ramValue = ramAvailable.NextValue();

        var totalMemoryBytes = GetTotalPhysicalMemory();
        var availableMb = ramValue;
        var totalMb = totalMemoryBytes / (1024.0 * 1024.0);
        var ramUsage = totalMb > 0
            ? Math.Round((totalMb - availableMb) / totalMb * 100, 2)
            : 0;

        var allDrives = DriveInfo.GetDrives()
            .Where(d => d.IsReady && d.DriveType == DriveType.Fixed);
        double totalDisk = 0, freeDisk = 0;
        foreach (var drive in allDrives)
        {
            totalDisk += drive.TotalSize;
            freeDisk += drive.AvailableFreeSpace;
        }
        var diskUsage = totalDisk > 0
            ? Math.Round((totalDisk - freeDisk) / totalDisk * 100, 2)
            : 0;

        var stats = new SystemStats
        {
            DeviceName = Environment.MachineName,
            CpuUsage = cpuUsage,
            RamUsage = ramUsage,
            RamAvailable = Math.Round(availableMb, 2),
            DiskUsage = diskUsage,
            DiskAvailable = Math.Round(freeDisk / (1024.0 * 1024.0 * 1024.0), 2),
            RecordedAt = DateTime.UtcNow
        };

        _context.SystemStats.Add(stats);
        await _context.SaveChangesAsync();

        await _securityLog.LogAsync(user.Id, user.Username, "STATS_COLLECTED",
            $"Собрана статистика системы: CPU={cpuUsage}%, RAM={ramUsage}%", true);

        return stats;
    }

    public async Task<List<SystemStats>> GetStatsHistoryAsync(string deviceName, int count = 10)
    {
        return await _context.SystemStats
            .Where(s => s.DeviceName == deviceName)
            .OrderByDescending(s => s.RecordedAt)
            .Take(count)
            .ToListAsync();
    }

    private static long GetTotalPhysicalMemory()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return 0;

        var memInfo = new PerformanceCounter("Memory", "Available MBytes");
        return (long)(memInfo.NextValue() * 1024 * 1024);
    }
}
