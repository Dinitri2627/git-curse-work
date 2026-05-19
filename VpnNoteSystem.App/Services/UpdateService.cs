using System.Net.Http;
using System.Text.Json;

namespace VpnNoteSystem.App.Services;

public class UpdateService : IUpdateService
{
    private readonly IAuthService _authService;
    private readonly ISecurityLogService _securityLog;
    private readonly string _versionUrl;
    private static readonly Version CurrentVersion = new(1, 0, 0);
    private Version? _latestVersion;

    public UpdateService(IAuthService authService, ISecurityLogService securityLog)
    {
        _authService = authService;
        _securityLog = securityLog;
        _versionUrl = "https://api.example.com/vpn-notes/version.json";
    }

    public async Task<bool> CheckForUpdatesAsync()
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

            var response = await client.GetStringAsync(_versionUrl);
            var versionInfo = JsonSerializer.Deserialize<VersionInfo>(response);

            if (versionInfo?.LatestVersion == null)
                return false;

            _latestVersion = new Version(versionInfo.LatestVersion);

            var result = _latestVersion > CurrentVersion;

            await _securityLog.LogAsync(null, "SYSTEM", "UPDATE_CHECK",
                $"Проверка обновлений: текущая={CurrentVersion}, последняя={_latestVersion}, " +
                $"доступно={result}", true);

            return result;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string> ApplyUpdateAsync()
    {
        var user = _authService.GetCurrentUser()
            ?? throw new UnauthorizedAccessException("Необходима аутентификация");

        if (_latestVersion == null)
        {
            var hasUpdate = await CheckForUpdatesAsync();
            if (!hasUpdate)
                return "[INFO] Нет доступных обновлений.";
        }

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            var downloadUrl = $"https://api.example.com/vpn-notes/download/v{_latestVersion}.zip";

            var zipBytes = await client.GetByteArrayAsync(downloadUrl);
            var updateDir = Path.Combine(AppContext.BaseDirectory, "update_temp");
            Directory.CreateDirectory(updateDir);

            var zipPath = Path.Combine(updateDir, "update.zip");
            await File.WriteAllBytesAsync(zipPath, zipBytes);

            await _securityLog.LogAsync(user.Id, user.Username, "UPDATE_APPLIED",
                $"Применено обновление {_latestVersion}", true);

            return $"Обновление до версии {_latestVersion} загружено. " +
                   "Для завершения перезапустите приложение.";
        }
        catch
        {
            await _securityLog.LogAsync(user.Id, user.Username, "UPDATE_FAILED",
                $"Ошибка при загрузке обновления {_latestVersion}", false);

            return "Ошибка при загрузке обновления.";
        }
    }

    private class VersionInfo
    {
        public string? LatestVersion { get; set; }
        public string? DownloadUrl { get; set; }
        public string? Changelog { get; set; }
    }
}
