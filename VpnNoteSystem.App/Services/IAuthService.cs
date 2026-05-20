using VpnNoteSystem.App.Models;

namespace VpnNoteSystem.App.Services;

public interface IAuthService
{
    Task<User?> LoginAsync(string username, string password);
    Task LogoutAsync();
    Task<(bool Success, string Message)> RegisterAsync(string username, string password);
    Task<List<User>> GetAllUsersAsync();
    User? GetCurrentUser();
    bool IsAuthenticated { get; }
}
