namespace VpnNoteSystem.App.Services;

public interface IUpdateService
{
    Task<bool> CheckForUpdatesAsync();
    Task<string> ApplyUpdateAsync();
}
