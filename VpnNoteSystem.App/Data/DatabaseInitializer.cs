using Microsoft.EntityFrameworkCore;
using VpnNoteSystem.App.Models;

namespace VpnNoteSystem.App.Data;

public class DatabaseInitializer
{
    private readonly AppDbContext _context;

    public DatabaseInitializer(AppDbContext context)
    {
        _context = context;
    }

    public async Task InitializeAsync()
    {
        await _context.Database.EnsureCreatedAsync();

        if (!await _context.Users.AnyAsync())
        {
            var admin = new User
            {
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                Role = "admin",
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };
            _context.Users.Add(admin);
            await _context.SaveChangesAsync();
        }
    }
}
