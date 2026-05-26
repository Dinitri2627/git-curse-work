using Microsoft.EntityFrameworkCore;
using VpnNoteSystem.App.Data;
using VpnNoteSystem.App.Models;

namespace VpnNoteSystem.App.Services;

public class NoteService : INoteService
{
    private readonly AppDbContext _context;
    private readonly IAuthService _authService;
    private readonly ISecurityLogService _securityLog;

    public NoteService(AppDbContext context, IAuthService authService,
        ISecurityLogService securityLog)
    {
        _context = context;
        _authService = authService;
        _securityLog = securityLog;
    }

    public async Task<Note> AddNoteAsync(string text)
    {
        var user = _authService.GetCurrentUser()
            ?? throw new UnauthorizedAccessException("Необходима аутентификация");

        var note = new Note
        {
            UserId = user.Id,
            Text = text.Replace("\0", ""),
            DeviceName = Environment.MachineName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Notes.Add(note);
        await _context.SaveChangesAsync();

        await _securityLog.LogAsync(user.Id, user.Username, "NOTE_CREATED",
            $"Создана заметка #{note.Id}", true);

        return note;
    }

    private bool IsAdmin => _authService.GetCurrentUser()?.Role == "admin";

    public async Task<List<Note>> GetAllNotesAsync()
    {
        var user = _authService.GetCurrentUser()
            ?? throw new UnauthorizedAccessException("Необходима аутентификация");

        return await _context.Notes
            .Where(n => n.UserId == user.Id && !n.IsDeleted)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Note>> GetUserNotesAsync(int userId)
    {
        var user = _authService.GetCurrentUser()
            ?? throw new UnauthorizedAccessException("Необходима аутентификация");

        if (user.Role != "admin")
            throw new UnauthorizedAccessException("Только администратор может просматривать заметки других пользователей");

        return await _context.Notes
            .Where(n => n.UserId == userId && !n.IsDeleted)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<Note?> GetNoteByIdAsync(int id)
    {
        var user = _authService.GetCurrentUser()
            ?? throw new UnauthorizedAccessException("Необходима аутентификация");

        if (IsAdmin)
            return await _context.Notes.FirstOrDefaultAsync(n => n.Id == id && !n.IsDeleted);

        return await _context.Notes
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == user.Id && !n.IsDeleted);
    }

    public async Task<bool> DeleteNoteAsync(int id)
    {
        var user = _authService.GetCurrentUser()
            ?? throw new UnauthorizedAccessException("Необходима аутентификация");

        Note? note;
        if (IsAdmin)
            note = await _context.Notes.FirstOrDefaultAsync(n => n.Id == id && !n.IsDeleted);
        else
            note = await _context.Notes.FirstOrDefaultAsync(n => n.Id == id && n.UserId == user.Id && !n.IsDeleted);

        if (note == null) return false;

        note.IsDeleted = true;
        await _context.SaveChangesAsync();

        await _securityLog.LogAsync(user.Id, user.Username, "NOTE_DELETED",
            $"Удалена заметка #{id}", true);

        return true;
    }
}
