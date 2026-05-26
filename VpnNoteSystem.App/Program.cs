using System.Runtime.InteropServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VpnNoteSystem.App.Commands;
using VpnNoteSystem.App.Data;
using VpnNoteSystem.App.Services;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

var dbSection = configuration.GetSection("Database");
var provider = dbSection["Provider"] ?? "SQLite";
var sqliteConn = dbSection["Sqlite"] ?? "Data Source=vpnnotes.db";
var pgConn = dbSection["PostgreSql"] ?? "";

var services = new ServiceCollection();

if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
{
    services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(pgConn));
}
else
{
    services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(sqliteConn));
}

services.AddScoped<DatabaseInitializer>();
services.AddScoped<ISecurityLogService, SecurityLogService>();
services.AddScoped<IAuthService, AuthService>();
services.AddScoped<INoteService, NoteService>();
services.AddScoped<IStatsService, StatsService>();
services.AddScoped<IUpdateService, UpdateService>();
services.AddScoped<CommandHandler>();

var serviceProvider = services.BuildServiceProvider();

try
{
    var initializer = serviceProvider.GetRequiredService<DatabaseInitializer>();
    await initializer.InitializeAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"  [ERROR] Ошибка подключения к БД: {ex.Message}");
    Console.WriteLine("  [INFO] Проверьте настройки в appsettings.json");
    Console.WriteLine();
}

Console.OutputEncoding = System.Text.Encoding.UTF8;

if (args.Length > 0)
{
    using var scope = serviceProvider.CreateScope();
    var handler = scope.ServiceProvider.GetRequiredService<CommandHandler>();
    await handler.HandleAsync(args);
}
else
{
    using var scope = serviceProvider.CreateScope();
    var handler = scope.ServiceProvider.GetRequiredService<CommandHandler>();
    await RunInteractiveMode(handler);
}

static async Task RunInteractiveMode(CommandHandler handler)
{
    Console.WriteLine("========================================");
    Console.WriteLine("  VPN Note System v1.0.0");
    Console.WriteLine("  Введите --help для списка команд");
    Console.WriteLine("  Введите exit для выхода");
    Console.WriteLine("========================================");
    Console.WriteLine();

    while (true)
    {
        Console.Write("> ");
        var input = ReadLineUnicode();

        if (string.IsNullOrWhiteSpace(input))
            continue;

        input = input.Trim();

        if (input.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
            input.Equals("quit", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("  [OK] До свидания!");
            break;
        }

        var parts = SplitArgs(input);
        await handler.HandleAsync(parts, input);
        Console.WriteLine();
    }
}

static string ReadLineUnicode()
{
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        try
        {
            var handle = GetStdHandle(-10);
            var buffer = new char[4096];
            if (ReadConsoleW(handle, buffer, 4096, out var read, IntPtr.Zero) && read > 0)
            {
                return new string(buffer, 0, (int)read).TrimEnd('\r', '\n');
            }
        }
        catch { }
    }

    return Console.ReadLine() ?? "";
}

static string[] SplitArgs(string input)
{
    var args = new List<string>();
    var inQuotes = false;
    var current = new System.Text.StringBuilder();

    for (int i = 0; i < input.Length; i++)
    {
        var c = input[i];

        if (c == '"')
        {
            inQuotes = !inQuotes;
            continue;
        }

        if (c == ' ' && !inQuotes)
        {
            if (current.Length > 0)
            {
                args.Add(current.ToString());
                current.Clear();
            }
        }
        else
        {
            current.Append(c);
        }
    }

    if (current.Length > 0)
        args.Add(current.ToString());

    return args.ToArray();
}

[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
static extern bool ReadConsoleW(IntPtr hConsoleInput, [Out] char[] lpBuffer, uint nNumberOfCharsToRead, out uint lpNumberOfCharsRead, IntPtr lpReserved);

[DllImport("kernel32.dll")]
static extern IntPtr GetStdHandle(int nStdHandle);
