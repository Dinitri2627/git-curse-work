using Microsoft.EntityFrameworkCore;
using Moq;
using VpnNoteSystem.App.Data;
using VpnNoteSystem.App.Models;
using VpnNoteSystem.App.Services;

namespace VpnNoteSystem.Tests;

public class NoteTests
{
    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"NoteTest_{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }

    private (NoteService, User) CreateNoteServiceWithAuth(AppDbContext context)
    {
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("pass"),
            Role = "user",
            IsActive = true
        };
        context.Users.Add(user);
        context.SaveChanges();

        var authMock = new Mock<IAuthService>();
        authMock.Setup(a => a.GetCurrentUser()).Returns(user);
        authMock.Setup(a => a.IsAuthenticated).Returns(true);

        var securityLogMock = new Mock<ISecurityLogService>();
        var noteService = new NoteService(context, authMock.Object, securityLogMock.Object);

        return (noteService, user);
    }

    [Fact]
    public async Task AddNoteAsync_ValidText_CreatesNote()
    {
        using var context = CreateContext();
        var (noteService, user) = CreateNoteServiceWithAuth(context);

        var note = await noteService.AddNoteAsync("Тестовая заметка");

        Assert.NotNull(note);
        Assert.Equal("Тестовая заметка", note.Text);
        Assert.Equal(user.Id, note.UserId);
        Assert.False(note.IsDeleted);
    }

    [Fact]
    public async Task AddNoteAsync_WithoutAuth_ThrowsException()
    {
        using var context = CreateContext();
        var authMock = new Mock<IAuthService>();
        var securityLogMock = new Mock<ISecurityLogService>();
        var noteService = new NoteService(context, authMock.Object, securityLogMock.Object);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => noteService.AddNoteAsync("test"));
    }

    [Fact]
    public async Task GetAllNotesAsync_ReturnsUserNotes()
    {
        using var context = CreateContext();
        var (noteService, user) = CreateNoteServiceWithAuth(context);

        await noteService.AddNoteAsync("Заметка 1");
        await noteService.AddNoteAsync("Заметка 2");
        await noteService.AddNoteAsync("Заметка 3");

        var notes = await noteService.GetAllNotesAsync();

        Assert.Equal(3, notes.Count);
    }

    [Fact]
    public async Task GetAllNotesAsync_Empty_ReturnsEmptyList()
    {
        using var context = CreateContext();
        var (noteService, _) = CreateNoteServiceWithAuth(context);

        var notes = await noteService.GetAllNotesAsync();

        Assert.Empty(notes);
    }

    [Fact]
    public async Task GetNoteByIdAsync_ExistingNote_ReturnsNote()
    {
        using var context = CreateContext();
        var (noteService, _) = CreateNoteServiceWithAuth(context);

        var created = await noteService.AddNoteAsync("Найти меня");
        var found = await noteService.GetNoteByIdAsync(created.Id);

        Assert.NotNull(found);
        Assert.Equal("Найти меня", found!.Text);
    }

    [Fact]
    public async Task GetNoteByIdAsync_NonExistent_ReturnsNull()
    {
        using var context = CreateContext();
        var (noteService, _) = CreateNoteServiceWithAuth(context);

        var found = await noteService.GetNoteByIdAsync(999);

        Assert.Null(found);
    }

    [Fact]
    public async Task DeleteNoteAsync_ExistingNote_SoftDeletes()
    {
        using var context = CreateContext();
        var (noteService, _) = CreateNoteServiceWithAuth(context);

        var created = await noteService.AddNoteAsync("Удалить меня");
        var result = await noteService.DeleteNoteAsync(created.Id);

        Assert.True(result);

        var deleted = await noteService.GetNoteByIdAsync(created.Id);
        Assert.Null(deleted);

        var allNotes = await noteService.GetAllNotesAsync();
        Assert.Empty(allNotes);
    }

    [Fact]
    public async Task DeleteNoteAsync_NonExistent_ReturnsFalse()
    {
        using var context = CreateContext();
        var (noteService, _) = CreateNoteServiceWithAuth(context);

        var result = await noteService.DeleteNoteAsync(999);

        Assert.False(result);
    }

    [Fact]
    public async Task MultipleUsers_NotesAreIsolated()
    {
        using var context = CreateContext();

        var user1 = new User { Id = 1, Username = "user1", PasswordHash = "h1", IsActive = true };
        var user2 = new User { Id = 2, Username = "user2", PasswordHash = "h2", IsActive = true };
        context.Users.AddRange(user1, user2);
        await context.SaveChangesAsync();

        var authMock1 = new Mock<IAuthService>();
        authMock1.Setup(a => a.GetCurrentUser()).Returns(user1);
        authMock1.Setup(a => a.IsAuthenticated).Returns(true);
        var securityLogMock = new Mock<ISecurityLogService>();
        var noteService1 = new NoteService(context, authMock1.Object, securityLogMock.Object);

        await noteService1.AddNoteAsync("Заметка пользователя 1");

        var authMock2 = new Mock<IAuthService>();
        authMock2.Setup(a => a.GetCurrentUser()).Returns(user2);
        authMock2.Setup(a => a.IsAuthenticated).Returns(true);
        var noteService2 = new NoteService(context, authMock2.Object, securityLogMock.Object);

        await noteService2.AddNoteAsync("Заметка пользователя 2");

        var notes1 = await noteService1.GetAllNotesAsync();
        var notes2 = await noteService2.GetAllNotesAsync();

        Assert.Single(notes1);
        Assert.Single(notes2);
        Assert.Equal("Заметка пользователя 1", notes1[0].Text);
        Assert.Equal("Заметка пользователя 2", notes2[0].Text);
    }
}
