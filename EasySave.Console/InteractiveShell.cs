using System.Text.Json;
using EasySave.Services;
using EasySave.Models;
using EasySave.Localization;

namespace EasySave.ConsoleApp;

public class InteractiveShell
{
    private readonly BackupService _service;
    private readonly ILocalizationService _loc;
    private readonly AppSettings _appSettings;
    private readonly string _settingsPath;

    public InteractiveShell(BackupService service, ILocalizationService loc, AppSettings appSettings, string settingsPath)
    {
        _service = service;
        _loc = loc;
        _appSettings = appSettings;
        _settingsPath = settingsPath;
    }

    public async Task Run()
    {
        while (true)
        {
            ShowMenu();
            var input = Console.ReadLine() ?? string.Empty;

            switch (input)
            {
                case "1": ListJobs(); break;
                case "2": CreateJob(); break;
                case "3": EditJob(); break;
                case "4": DeleteJob(); break;
                case "5": await RunJob(); break;
                case "6": await RunAll(); break;
                case "7": ShowSettings(); break;
                case "q": return;
                default: Console.WriteLine(_loc.Get("invalid")); break;
            }
        }
    }

    private void ShowMenu()
    {
        Console.WriteLine("==== " + _loc.Get("menu_title") + " ====");
        Console.WriteLine("1) " + _loc.Get("menu_list"));
        Console.WriteLine("2) " + _loc.Get("menu_create"));
        Console.WriteLine("3) " + _loc.Get("menu_edit"));
        Console.WriteLine("4) " + _loc.Get("menu_delete"));
        Console.WriteLine("5) " + _loc.Get("menu_run"));
        Console.WriteLine("6) " + _loc.Get("menu_run_all"));
        Console.WriteLine("7) " + _loc.Get("menu_settings"));
        Console.WriteLine("q) " + _loc.Get("menu_quit"));
    }

    private void ListJobs()
    {
        var jobs = _service.GetJobs().ToList();

        for (int i = 0; i < jobs.Count; i++)
            Console.WriteLine($"{i + 1}. {jobs[i].Name} [{jobs[i].Type}]");
    }

    private void CreateJob()
    {
        Console.Write(_loc.Get("prompt_name"));
        var name = Console.ReadLine() ?? string.Empty;

        Console.Write(_loc.Get("prompt_source"));
        var source = Console.ReadLine() ?? string.Empty;

        Console.Write(_loc.Get("prompt_target"));
        var target = Console.ReadLine() ?? string.Empty;

        Console.Write(_loc.Get("prompt_type"));
        var type = (Console.ReadLine() ?? string.Empty) == "2"
            ? BackupType.Differential
            : BackupType.Full;

        _service.AddJob(new BackupJobConfig
        {
            Name = name,
            SourceDir = source,
            TargetDir = target,
            Type = type,
            IsActive = true
        });
    }

    private void EditJob()
    {
        try
        {
            ListJobs();

            Console.Write(_loc.Get("prompt_select"));
            if (!int.TryParse(Console.ReadLine() ?? string.Empty, out int n))
            {
                Console.WriteLine(_loc.Get("invalid"));
                return;
            }
            int index = n - 1;

            var jobs = _service.GetJobs().ToList();
            var job = jobs[index];

            Console.Write($"{_loc.Get("prompt_name")} ({job.Name}): ");
            var name = Console.ReadLine() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(name))
                job.Name = name;

            Console.Write($"{_loc.Get("prompt_source")} ({job.SourceDir}): ");
            var source = Console.ReadLine() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(source))
                job.SourceDir = source;

            Console.Write($"{_loc.Get("prompt_target")} ({job.TargetDir}): ");
            var target = Console.ReadLine() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(target))
                job.TargetDir = target;

            Console.Write($"{_loc.Get("prompt_type")} ({job.Type}): ");
            var typeInput = Console.ReadLine() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(typeInput))
                job.Type = typeInput == "2" ? BackupType.Differential : BackupType.Full;

            _service.UpdateJob(index, job);
        }
        catch (ArgumentOutOfRangeException)
        {
            Console.WriteLine(_loc.Get("error_invalid_index"));
        }
    }

    private void DeleteJob()
    {
        try
        {
            ListJobs();

            Console.Write(_loc.Get("prompt_select"));
            if (!int.TryParse(Console.ReadLine() ?? string.Empty, out int n))
            {
                Console.WriteLine(_loc.Get("invalid"));
                return;
            }

            _service.RemoveJob(n - 1);
        }
        catch (ArgumentOutOfRangeException)
        {
            Console.WriteLine(_loc.Get("error_invalid_index"));
        }
    }

    private async Task RunJob()
    {
        try
        {
            ListJobs();

            Console.Write(_loc.Get("prompt_select"));
            if (!int.TryParse(Console.ReadLine() ?? string.Empty, out int n))
            {
                Console.WriteLine(_loc.Get("invalid"));
                return;
            }

            await _service.RunJob(n - 1);
        }
        catch (ArgumentOutOfRangeException)
        {
            Console.WriteLine(_loc.Get("error_invalid_index"));
        }
    }

    private async Task RunAll()
    {
        try
        {
            var count = _service.GetJobs().Count();
            await _service.RunRange(Enumerable.Range(0, count));
        }
        catch (ArgumentOutOfRangeException)
        {
            Console.WriteLine(_loc.Get("error_invalid_index"));
        }
    }

    private void ShowSettings()
    {
        Console.WriteLine("=== " + _loc.Get("menu_settings") + " ===");
        Console.WriteLine(_loc.Get("settings_current_format") + ": " + _appSettings.LogFormat);
        Console.WriteLine("1 - JSON");
        Console.WriteLine("2 - XML");
        Console.Write(_loc.Get("settings_choose_format"));
        var input = Console.ReadLine() ?? string.Empty;

        LogFormat? format = input switch
        {
            "1" => LogFormat.Json,
            "2" => LogFormat.Xml,
            _ => null
        };

        if (format is null)
        {
            Console.WriteLine(_loc.Get("settings_invalid"));
            return;
        }

        _appSettings.LogFormat = format.Value;
        File.WriteAllText(_settingsPath, JsonSerializer.Serialize(_appSettings, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine(_loc.Get("settings_format_saved"));
    }
}
