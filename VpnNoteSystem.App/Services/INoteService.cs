using VpnNoteSystem.App.Models;

namespace VpnNoteSystem.App.Services;

public interface INoteService
{
    Task<Note> AddNoteAsync(string text);
    Task<List<Note>> GetAllNotesAsync();
    Task<List<Note>> GetUserNotesAsync(int userId);
    Task<Note?> GetNoteByIdAsync(int id);
    Task<bool> DeleteNoteAsync(int id);
    Task<(bool Success, string Message)> UpdateNoteAsync(int id, string newText);
}
