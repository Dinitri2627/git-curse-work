namespace VpnNoteSystem.App.Models;

public class SecurityLog
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
