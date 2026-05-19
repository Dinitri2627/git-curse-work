using Microsoft.EntityFrameworkCore;
using VpnNoteSystem.App.Models;

namespace VpnNoteSystem.App.Data;

public class AppDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Note> Notes { get; set; }
    public DbSet<SystemStats> SystemStats { get; set; }
    public DbSet<SecurityLog> SecurityLogs { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.Username).HasMaxLength(100).IsRequired();
            entity.Property(e => e.PasswordHash).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Role).HasMaxLength(50).HasDefaultValue("user");
        });

        modelBuilder.Entity<Note>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Text).IsRequired();
            entity.Property(e => e.DeviceName).HasMaxLength(200);
            entity.HasOne<User>().WithMany().HasForeignKey(e => e.UserId);
        });

        modelBuilder.Entity<SystemStats>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DeviceName).HasMaxLength(200).IsRequired();
            entity.ToTable("SystemStats");
        });

        modelBuilder.Entity<SecurityLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Username).HasMaxLength(100);
            entity.Property(e => e.IpAddress).HasMaxLength(50);
            entity.ToTable("SecurityLogs");
        });
    }
}
