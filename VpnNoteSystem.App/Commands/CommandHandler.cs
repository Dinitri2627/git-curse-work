using VpnNoteSystem.App.Services;

namespace VpnNoteSystem.App.Commands;

public class CommandHandler
{
    private readonly IAuthService _authService;
    private readonly INoteService _noteService;
    private readonly IStatsService _statsService;
    private readonly IUpdateService _updateService;
    private readonly ISecurityLogService _securityLog;

    public CommandHandler(
        IAuthService authService,
        INoteService noteService,
        IStatsService statsService,
        IUpdateService updateService,
        ISecurityLogService securityLog)
    {
        _authService = authService;
        _noteService = noteService;
        _statsService = statsService;
        _updateService = updateService;
        _securityLog = securityLog;
    }

    public async Task<bool> HandleAsync(string[] args)
    {
        if (args.Length == 0)
        {
            ShowBanner();
            return true;
        }

        var command = args[0].ToLower();

        try
        {
            return command switch
            {
                "--help" or "-h" or "--guide" => ShowHelp(),
                "--register" => await HandleRegisterAsync(args),
                "--login" => await HandleLoginAsync(args),
                "--logout" => await HandleLogoutAsync(),
                "--listusers" => await HandleListUsersAsync(),
                "--addnewnote" => await HandleAddNoteAsync(args),
                "--listnotes" => await HandleListNotesAsync(),
                "--getnote" => await HandleGetNoteAsync(args),
                "--deletenote" => await HandleDeleteNoteAsync(args),
                "--stats" => await HandleStatsAsync(),
                "--securitylogs" => await HandleSecurityLogsAsync(),
                "--checkupdate" => await HandleCheckUpdateAsync(),
                "--update" => await HandleApplyUpdateAsync(),
                _ => ShowUnknownCommand(command)
            };
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.WriteLine($"  [ERROR] {ex.Message}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [ERROR] {ex.Message}");
            if (ex.InnerException != null)
                Console.WriteLine($"  [DETAIL] {ex.InnerException.Message}");
            return true;
        }
    }

    private void ShowBanner()
    {
        Console.WriteLine("========================================");
        Console.WriteLine("  VPN Note System v1.0.0");
        Console.WriteLine("  Консольная система заметок");
        Console.WriteLine("========================================");
        Console.WriteLine("  Используйте --help для списка команд");
        Console.WriteLine();
    }

    private bool ShowHelp()
    {
        var helpPath = Path.Combine(AppContext.BaseDirectory, "Help", "help.md");
        if (File.Exists(helpPath))
        {
            Console.WriteLine(File.ReadAllText(helpPath));
        }
        else
        {
            var devHelp = Path.Combine(Directory.GetCurrentDirectory(), "Help", "help.md");
            if (File.Exists(devHelp))
                Console.WriteLine(File.ReadAllText(devHelp));
            else
                ShowDefaultHelp();
        }
        return true;
    }

    private void ShowDefaultHelp()
    {
        Console.WriteLine("""
# VPN Note System - Справка

## Команды

### Аутентификация
| Команда | Описание | Пример |
|---------|----------|--------|
                    | `--register <username> <password>` | Регистрация нового пользователя | `--register user1 pass123` |
                    | `--login <username> <password>` | Вход в систему | `--login user1 pass123` |
                    | `--logout` | Выход из системы | `--logout` |
                    | `--listusers` | Список всех пользователей (только admin) | `--listusers` |

### Работа с заметками
| Команда | Описание | Пример |
|---------|----------|--------|
| `--addNewNote "текст заметки"` | Создать заметку | `--addNewNote "Настроить VPN"` |
| `--listNotes` | Список заметок | `--listNotes` |
| `--getNote <id>` | Просмотр заметки | `--getNote 1` |
| `--deleteNote <id>` | Удалить заметку | `--deleteNote 1` |

### Статистика системы
| Команда | Описание | Пример |
|---------|----------|--------|
| `--stats` | Статистика CPU/RAM/HDD | `--stats` |

### Логи безопасности
| Команда | Описание | Пример |
|---------|----------|--------|
| `--securityLogs` | Просмотр логов | `--securityLogs` |

### Обновления
| Команда | Описание | Пример |
|---------|----------|--------|
| `--checkUpdate` | Проверить обновления | `--checkUpdate` |
| `--update` | Установить обновление | `--update` |

### Справка
| Команда | Описание | Пример |
|---------|----------|--------|
| `--help` | Показать справку | `--help` |

---
""");
    }

    private async Task<bool> HandleRegisterAsync(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("  Использование: --register <username> <password>");
            return true;
        }

        var (success, message) = await _authService.RegisterAsync(args[1], args[2]);
        Console.WriteLine(success ? $"  [OK] {message}" : $"  [ERROR] {message}");
        return true;
    }

    private async Task<bool> HandleLoginAsync(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("  Использование: --login <username> <password>");
            return true;
        }

        var user = await _authService.LoginAsync(args[1], args[2]);
        if (user != null)
        {
            Console.WriteLine($"  [OK] Вход выполнен: {user.Username} (роль: {user.Role})");
        }
        else
        {
            Console.WriteLine("  [ERROR] Неверное имя пользователя или пароль");
        }
        return true;
    }

    private async Task<bool> HandleLogoutAsync()
    {
        await _authService.LogoutAsync();
        Console.WriteLine("  [OK] Выполнен выход из системы");
        return true;
    }

    private async Task<bool> HandleListUsersAsync()
    {
        try
        {
            var users = await _authService.GetAllUsersAsync();
            Console.WriteLine($"  Пользователи ({users.Count}):");
            Console.WriteLine($"  {"ID",-4} {"Логин",-20} {"Роль",-10} {"Активен",-10} {"Создан",-20}");
            Console.WriteLine("  " + new string('-', 64));
            foreach (var u in users)
            {
                Console.WriteLine($"  {u.Id,-4} {u.Username,-20} {u.Role,-10} {(u.IsActive ? "Да" : "Нет"),-10} {u.CreatedAt:dd.MM.yyyy HH:mm}");
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.WriteLine($"  [ERROR] {ex.Message}");
        }
        return true;
    }

    private async Task<bool> HandleAddNoteAsync(string[] args)
    {
        if (!_authService.IsAuthenticated)
        {
            Console.WriteLine("  [ERROR] Необходима аутентификация. Используйте --login");
            return true;
        }

        var text = string.Join(" ", args.Skip(1));
        if (string.IsNullOrWhiteSpace(text))
        {
            Console.WriteLine("  Использование: --addNewNote \"текст заметки\"");
            return true;
        }

        var note = await _noteService.AddNoteAsync(text);
        Console.WriteLine($"  [OK] Заметка #{note.Id} создана");
        return true;
    }

    private async Task<bool> HandleListNotesAsync()
    {
        if (!_authService.IsAuthenticated)
        {
            Console.WriteLine("  [ERROR] Необходима аутентификация. Используйте --login");
            return true;
        }

        var notes = await _noteService.GetAllNotesAsync();
        if (notes.Count == 0)
        {
            Console.WriteLine("  [INFO] Заметок нет");
            return true;
        }

        Console.WriteLine($"  Заметки ({notes.Count}):");
        foreach (var note in notes)
        {
            var preview = note.Text.Length > 60
                ? note.Text[..60] + "..."
                : note.Text;
            Console.WriteLine($"  #{note.Id} [{note.CreatedAt:dd.MM.yyyy HH:mm}] {preview}");
        }
        return true;
    }

    private async Task<bool> HandleGetNoteAsync(string[] args)
    {
        if (!_authService.IsAuthenticated)
        {
            Console.WriteLine("  [ERROR] Необходима аутентификация. Используйте --login");
            return true;
        }

        if (args.Length < 2 || !int.TryParse(args[1], out var id))
        {
            Console.WriteLine("  Использование: --getNote <id>");
            return true;
        }

        var note = await _noteService.GetNoteByIdAsync(id);
        if (note == null)
        {
            Console.WriteLine("  [ERROR] Заметка не найдена");
            return true;
        }

        Console.WriteLine($"  Заметка #{note.Id}");
        Console.WriteLine($"  Дата: {note.CreatedAt:dd.MM.yyyy HH:mm}");
        Console.WriteLine($"  Устройство: {note.DeviceName}");
        Console.WriteLine($"  Текст: {note.Text}");
        return true;
    }

    private async Task<bool> HandleDeleteNoteAsync(string[] args)
    {
        if (!_authService.IsAuthenticated)
        {
            Console.WriteLine("  [ERROR] Необходима аутентификация. Используйте --login");
            return true;
        }

        if (args.Length < 2 || !int.TryParse(args[1], out var id))
        {
            Console.WriteLine("  Использование: --deleteNote <id>");
            return true;
        }

        var success = await _noteService.DeleteNoteAsync(id);
        Console.WriteLine(success
            ? $"  [OK] Заметка #{id} удалена"
            : "  [ERROR] Заметка не найдена");
        return true;
    }

    private async Task<bool> HandleStatsAsync()
    {
        if (!_authService.IsAuthenticated)
        {
            Console.WriteLine("  [ERROR] Необходима аутентификация. Используйте --login");
            return true;
        }

        var stats = await _statsService.CollectCurrentStatsAsync();
        Console.WriteLine($"  Статистика устройства: {stats.DeviceName}");
        Console.WriteLine($"  CPU: {stats.CpuUsage}%");
        Console.WriteLine($"  RAM: {stats.RamUsage}% (доступно: {stats.RamAvailable} MB)");
        Console.WriteLine($"  HDD: {stats.DiskUsage}% (доступно: {stats.DiskAvailable} GB)");
        Console.WriteLine($"  Записано: {stats.RecordedAt:dd.MM.yyyy HH:mm}");
        return true;
    }

    private async Task<bool> HandleSecurityLogsAsync()
    {
        if (!_authService.IsAuthenticated)
        {
            Console.WriteLine("  [ERROR] Необходима аутентификация. Используйте --login");
            return true;
        }

        var logs = await _securityLog.GetRecentLogsAsync();
        if (logs.Count == 0)
        {
            Console.WriteLine("  [INFO] Логи безопасности отсутствуют");
            return true;
        }

        Console.WriteLine("  Последние логи безопасности:");
        Console.WriteLine("  " + new string('-', 80));
        foreach (var log in logs)
        {
            var status = log.IsSuccess ? "OK" : "FAIL";
            Console.WriteLine($"  [{log.Timestamp:dd.MM.yyyy HH:mm:ss}] " +
                $"[{status}] {log.Action} - {log.Username}: {log.Details}");
        }
        return true;
    }

    private async Task<bool> HandleCheckUpdateAsync()
    {
        Console.WriteLine("  [INFO] Проверка обновлений...");
        var hasUpdate = await _updateService.CheckForUpdatesAsync();
        Console.WriteLine(hasUpdate
            ? "  [OK] Доступно обновление. Используйте --update для установки"
            : "  [INFO] Сервер обновлений недоступен");

        return true;
    }

    private async Task<bool> HandleApplyUpdateAsync()
    {
        if (!_authService.IsAuthenticated)
        {
            Console.WriteLine("  [ERROR] Необходима аутентификация. Используйте --login");
            return true;
        }

        var result = await _updateService.ApplyUpdateAsync();
        Console.WriteLine($"  {result}");
        return true;
    }

    private bool ShowUnknownCommand(string command)
    {
        Console.WriteLine($"  [ERROR] Неизвестная команда: {command}");
        Console.WriteLine("  Используйте --help для списка команд");
        return true;
    }
}
