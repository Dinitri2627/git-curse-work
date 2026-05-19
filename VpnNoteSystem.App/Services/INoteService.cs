using VpnNoteSystem.App.Models;

namespace VpnNoteSystem.App.Services;

public interface INoteService
{
    Task<Note> AddNoteAsync(string text);
    Task<List<Note>> GetAllNotesAsync();
    Task<Note?> GetNoteByIdAsync(int id);
    Task<bool> DeleteNoteAsync(int id);
}
