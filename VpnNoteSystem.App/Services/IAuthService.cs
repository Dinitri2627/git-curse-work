using VpnNoteSystem.App.Models;

namespace VpnNoteSystem.App.Services;

public interface IAuthService
{
    Task<User?> LoginAsync(string username, string password);
    Task LogoutAsync();
    Task<(bool Success, string Message)> RegisterAsync(string username, string password);
    Task<List<User>> GetAllUsersAsync();
    Task<(bool Success, string Message)> DeleteUserAsync(int userId);
    Task<(bool Success, string Message)> CreateAdminAsync(string username, string password);
    User? GetCurrentUser();
    bool IsAuthenticated { get; }
}
