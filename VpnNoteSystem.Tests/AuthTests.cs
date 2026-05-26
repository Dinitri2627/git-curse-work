using Microsoft.EntityFrameworkCore;
using Moq;
using VpnNoteSystem.App.Data;
using VpnNoteSystem.App.Models;
using VpnNoteSystem.App.Services;

namespace VpnNoteSystem.Tests;

public class AuthTests
{
    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"AuthTest_{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsUser()
    {
        using var context = CreateContext();
        var admin = new User
        {
            Username = "testuser",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("testpass"),
            Role = "user",
            IsActive = true
        };
        context.Users.Add(admin);
        await context.SaveChangesAsync();

        var securityLogMock = new Mock<ISecurityLogService>();
        var authService = new AuthService(context, securityLogMock.Object);

        var result = await authService.LoginAsync("testuser", "testpass");

        Assert.NotNull(result);
        Assert.Equal("testuser", result!.Username);
        Assert.True(authService.IsAuthenticated);
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ReturnsNull()
    {
        using var context = CreateContext();
        var admin = new User
        {
            Username = "testuser",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("correctpass"),
            Role = "user",
            IsActive = true
        };
        context.Users.Add(admin);
        await context.SaveChangesAsync();

        var securityLogMock = new Mock<ISecurityLogService>();
        var authService = new AuthService(context, securityLogMock.Object);

        var result = await authService.LoginAsync("testuser", "wrongpass");

        Assert.Null(result);
        Assert.False(authService.IsAuthenticated);
    }

    [Fact]
    public async Task LoginAsync_NonexistentUser_ReturnsNull()
    {
        using var context = CreateContext();
        var securityLogMock = new Mock<ISecurityLogService>();
        var authService = new AuthService(context, securityLogMock.Object);

        var result = await authService.LoginAsync("nobody", "password");

        Assert.Null(result);
        Assert.False(authService.IsAuthenticated);
    }

    [Fact]
    public async Task LoginAsync_InactiveUser_ReturnsNull()
    {
        using var context = CreateContext();
        var inactiveUser = new User
        {
            Username = "inactive",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("pass"),
            Role = "user",
            IsActive = false
        };
        context.Users.Add(inactiveUser);
        await context.SaveChangesAsync();

        var securityLogMock = new Mock<ISecurityLogService>();
        var authService = new AuthService(context, securityLogMock.Object);

        var result = await authService.LoginAsync("inactive", "pass");

        Assert.Null(result);
    }

    [Fact]
    public async Task Logout_ClearsCurrentUser()
    {
        using var context = CreateContext();
        var user = new User
        {
            Username = "logoutuser",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("pass"),
            IsActive = true
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var securityLogMock = new Mock<ISecurityLogService>();
        var authService = new AuthService(context, securityLogMock.Object);

        await authService.LoginAsync("logoutuser", "pass");
        Assert.True(authService.IsAuthenticated);

        await authService.LogoutAsync();
        Assert.False(authService.IsAuthenticated);
    }

    [Fact]
    public async Task RegisterAsync_NewUser_ReturnsSuccess()
    {
        using var context = CreateContext();
        var securityLogMock = new Mock<ISecurityLogService>();
        var authService = new AuthService(context, securityLogMock.Object);

        var (success, message) = await authService.RegisterAsync("newuser", "pass123");

        Assert.True(success);
        Assert.Contains("успешна", message);

        var user = await context.Users.FirstOrDefaultAsync(u => u.Username == "newuser");
        Assert.NotNull(user);
        Assert.Equal("user", user!.Role);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateUser_ReturnsError()
    {
        using var context = CreateContext();
        context.Users.Add(new User
        {
            Username = "existing",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("pass"),
            IsActive = true
        });
        await context.SaveChangesAsync();

        var securityLogMock = new Mock<ISecurityLogService>();
        var authService = new AuthService(context, securityLogMock.Object);

        var (success, message) = await authService.RegisterAsync("existing", "pass123");

        Assert.False(success);
        Assert.Contains("уже существует", message);
    }

    [Fact]
    public async Task RegisterAsync_ShortUsername_ReturnsError()
    {
        using var context = CreateContext();
        var securityLogMock = new Mock<ISecurityLogService>();
        var authService = new AuthService(context, securityLogMock.Object);

        var (success, message) = await authService.RegisterAsync("ab", "pass123");

        Assert.False(success);
    }

    [Fact]
    public async Task GetAllUsersAsync_Admin_ReturnsAllUsers()
    {
        using var context = CreateContext();
        context.Users.Add(new User { Username = "admin", PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"), Role = "admin", IsActive = true });
        context.Users.Add(new User { Username = "user1", PasswordHash = BCrypt.Net.BCrypt.HashPassword("pass1"), Role = "user", IsActive = true });
        context.Users.Add(new User { Username = "user2", PasswordHash = BCrypt.Net.BCrypt.HashPassword("pass2"), Role = "user", IsActive = true });
        await context.SaveChangesAsync();

        var securityLogMock = new Mock<ISecurityLogService>();
        var authService = new AuthService(context, securityLogMock.Object);
        await authService.LoginAsync("admin", "admin123");

        var users = await authService.GetAllUsersAsync();

        Assert.Equal(3, users.Count);
    }

    [Fact]
    public async Task GetAllUsersAsync_NonAdmin_ThrowsException()
    {
        using var context = CreateContext();
        context.Users.Add(new User { Username = "regular", PasswordHash = BCrypt.Net.BCrypt.HashPassword("pass"), Role = "user", IsActive = true });
        await context.SaveChangesAsync();

        var securityLogMock = new Mock<ISecurityLogService>();
        var authService = new AuthService(context, securityLogMock.Object);
        await authService.LoginAsync("regular", "pass");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => authService.GetAllUsersAsync());
    }

    [Fact]
    public async Task GetAllUsersAsync_WithoutAuth_ThrowsException()
    {
        using var context = CreateContext();
        var securityLogMock = new Mock<ISecurityLogService>();
        var authService = new AuthService(context, securityLogMock.Object);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => authService.GetAllUsersAsync());
    }

    [Fact]
    public async Task RegisterAsync_ShortPassword_ReturnsError()
    {
        using var context = CreateContext();
        var securityLogMock = new Mock<ISecurityLogService>();
        var authService = new AuthService(context, securityLogMock.Object);

        var (success, message) = await authService.RegisterAsync("validuser", "12");

        Assert.False(success);
    }

    [Fact]
    public async Task CreateAdminAsync_Admin_ReturnsSuccess()
    {
        using var context = CreateContext();
        context.Users.Add(new User { Username = "admin", PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"), Role = "admin", IsActive = true });
        await context.SaveChangesAsync();

        var securityLogMock = new Mock<ISecurityLogService>();
        var authService = new AuthService(context, securityLogMock.Object);
        await authService.LoginAsync("admin", "admin123");

        var (success, message) = await authService.CreateAdminAsync("newadmin", "admin456");

        Assert.True(success);
        var saved = await context.Users.FirstOrDefaultAsync(u => u.Username == "newadmin");
        Assert.NotNull(saved);
        Assert.Equal("admin", saved!.Role);
    }

    [Fact]
    public async Task CreateAdminAsync_NonAdmin_ThrowsException()
    {
        using var context = CreateContext();
        context.Users.Add(new User { Username = "user1", PasswordHash = BCrypt.Net.BCrypt.HashPassword("pass"), Role = "user", IsActive = true });
        await context.SaveChangesAsync();

        var securityLogMock = new Mock<ISecurityLogService>();
        var authService = new AuthService(context, securityLogMock.Object);
        await authService.LoginAsync("user1", "pass");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => authService.CreateAdminAsync("hacker", "pass123"));
    }

    [Fact]
    public async Task DeleteUserAsync_Admin_DeactivatesUser()
    {
        using var context = CreateContext();
        context.Users.Add(new User { Id = 1, Username = "admin", PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"), Role = "admin", IsActive = true });
        context.Users.Add(new User { Id = 2, Username = "todelete", PasswordHash = "hash", Role = "user", IsActive = true });
        context.Notes.Add(new Note { Id = 1, UserId = 2, Text = "note", DeviceName = "pc", CreatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var securityLogMock = new Mock<ISecurityLogService>();
        var authService = new AuthService(context, securityLogMock.Object);
        await authService.LoginAsync("admin", "admin123");

        var (success, message) = await authService.DeleteUserAsync(2);

        Assert.True(success);
        var deletedUser = await context.Users.FindAsync(2);
        Assert.False(deletedUser!.IsActive);
    }

    [Fact]
    public async Task DeleteUserAsync_NonAdmin_ThrowsException()
    {
        using var context = CreateContext();
        context.Users.Add(new User { Username = "user1", PasswordHash = BCrypt.Net.BCrypt.HashPassword("pass"), Role = "user", IsActive = true });
        await context.SaveChangesAsync();

        var securityLogMock = new Mock<ISecurityLogService>();
        var authService = new AuthService(context, securityLogMock.Object);
        await authService.LoginAsync("user1", "pass");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => authService.DeleteUserAsync(999));
    }

    [Fact]
    public async Task DeleteUserAsync_Self_ReturnsError()
    {
        using var context = CreateContext();
        context.Users.Add(new User { Id = 1, Username = "admin", PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"), Role = "admin", IsActive = true });
        await context.SaveChangesAsync();

        var securityLogMock = new Mock<ISecurityLogService>();
        var authService = new AuthService(context, securityLogMock.Object);
        await authService.LoginAsync("admin", "admin123");

        var (success, message) = await authService.DeleteUserAsync(1);

        Assert.False(success);
        Assert.Contains("самого себя", message);
    }
}
