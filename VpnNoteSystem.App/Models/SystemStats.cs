namespace VpnNoteSystem.App.Models;

public class SystemStats
{
    public int Id { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public double CpuUsage { get; set; }
    public double RamUsage { get; set; }
    public double RamAvailable { get; set; }
    public double DiskUsage { get; set; }
    public double DiskAvailable { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}
