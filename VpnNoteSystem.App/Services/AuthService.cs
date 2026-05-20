using Microsoft.EntityFrameworkCore;
using VpnNoteSystem.App.Data;
using VpnNoteSystem.App.Models;

namespace VpnNoteSystem.App.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly ISecurityLogService _securityLog;
    private User? _currentUser;

    public AuthService(AppDbContext context, ISecurityLogService securityLog)
    {
        _context = context;
        _securityLog = securityLog;
    }

    public async Task<User?> LoginAsync(string username, string password)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u =>
            u.Username == username && u.IsActive);

        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            await _securityLog.LogAsync(null, username, "LOGIN_FAILED",
                $"Неудачная попытка входа", false);
            return null;
        }

        _currentUser = user;

        await _securityLog.LogAsync(user.Id, user.Username, "LOGIN_SUCCESS",
            $"Успешный вход в систему", true);

        return user;
    }

    public async Task<(bool Success, string Message)> RegisterAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
            return (false, "Имя пользователя должно быть от 3 символов");

        if (string.IsNullOrWhiteSpace(password) || password.Length < 4)
            return (false, "Пароль должен быть от 4 символов");

        var exists = await _context.Users.AnyAsync(u => u.Username == username);
        if (exists)
            return (false, "Пользователь с таким именем уже существует");

        var user = new User
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = "user",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        await _securityLog.LogAsync(user.Id, user.Username, "REGISTER",
            $"Зарегистрирован новый пользователь", true);

        return (true, "Регистрация успешна");
    }

    public Task LogoutAsync()
    {
        if (_currentUser != null)
        {
            _securityLog.LogAsync(_currentUser.Id, _currentUser.Username,
                "LOGOUT", "Выход из системы", true).ConfigureAwait(false);
        }

        _currentUser = null;
        return Task.CompletedTask;
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        if (_currentUser == null)
            throw new UnauthorizedAccessException("Необходима аутентификация");

        if (_currentUser.Role != "admin")
            throw new UnauthorizedAccessException("Только администратор может просматривать список пользователей");

        return await _context.Users.ToListAsync();
    }

    public User? GetCurrentUser() => _currentUser;
    public bool IsAuthenticated => _currentUser != null;
}
