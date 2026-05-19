using Microsoft.EntityFrameworkCore;
using VpnNoteSystem.App.Data;
using VpnNoteSystem.App.Models;

namespace VpnNoteSystem.Tests;

public class DatabaseTests
{
    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"DBTest_{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task DatabaseInitializer_AddsAdminUser()
    {
        using var context = CreateContext();
        var initializer = new DatabaseInitializer(context);
        await initializer.InitializeAsync();

        var admin = await context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
        Assert.NotNull(admin);
        Assert.Equal("admin", admin!.Username);
        Assert.Equal("admin", admin.Role);
        Assert.True(admin.IsActive);
    }

    [Fact]
    public async Task DatabaseInitializer_Idempotent_NoDuplicateAdmin()
    {
        using var context = CreateContext();
        var initializer = new DatabaseInitializer(context);
        await initializer.InitializeAsync();
        await initializer.InitializeAsync();

        var adminCount = await context.Users.CountAsync(u => u.Username == "admin");
        Assert.Equal(1, adminCount);
    }

    [Fact]
    public async Task CanCreateAndReadUsers()
    {
        using var context = CreateContext();
        var user = new User
        {
            Username = "test_user",
            PasswordHash = "hash123",
            Role = "user",
            IsActive = true
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var saved = await context.Users.FirstOrDefaultAsync(u => u.Username == "test_user");
        Assert.NotNull(saved);
        Assert.Equal("hash123", saved!.PasswordHash);
    }

    [Fact]
    public async Task CanCreateAndReadNotes()
    {
        using var context = CreateContext();
        var user = new User { Username = "noteuser", PasswordHash = "hash", IsActive = true };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var note = new Note
        {
            UserId = user.Id,
            Text = "Тестовая заметка для БД",
            DeviceName = "test-pc",
            CreatedAt = DateTime.UtcNow
        };
        context.Notes.Add(note);
        await context.SaveChangesAsync();

        var saved = await context.Notes.FirstOrDefaultAsync(n => n.Id == note.Id);
        Assert.NotNull(saved);
        Assert.Equal("Тестовая заметка для БД", saved!.Text);
        Assert.Equal(user.Id, saved.UserId);
    }

    [Fact]
    public async Task CanCreateAndReadSecurityLogs()
    {
        using var context = CreateContext();
        var log = new SecurityLog
        {
            Username = "admin",
            Action = "LOGIN_SUCCESS",
            Details = "Test login",
            IpAddress = "127.0.0.1",
            IsSuccess = true,
            Timestamp = DateTime.UtcNow
        };
        context.SecurityLogs.Add(log);
        await context.SaveChangesAsync();

        var saved = await context.SecurityLogs.FirstOrDefaultAsync(l => l.Id == log.Id);
        Assert.NotNull(saved);
        Assert.Equal("LOGIN_SUCCESS", saved!.Action);
        Assert.True(saved.IsSuccess);
    }

    [Fact]
    public async Task CanCreateAndReadSystemStats()
    {
        using var context = CreateContext();
        var stats = new SystemStats
        {
            DeviceName = "server-01",
            CpuUsage = 45.5,
            RamUsage = 62.3,
            RamAvailable = 8192,
            DiskUsage = 55.0,
            DiskAvailable = 100.5,
            RecordedAt = DateTime.UtcNow
        };
        context.SystemStats.Add(stats);
        await context.SaveChangesAsync();

        var saved = await context.SystemStats.FirstOrDefaultAsync(s => s.Id == stats.Id);
        Assert.NotNull(saved);
        Assert.Equal("server-01", saved!.DeviceName);
        Assert.Equal(45.5, saved.CpuUsage);
    }
}
