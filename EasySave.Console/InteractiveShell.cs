using EasySave.Services;
using EasySave.Models;
using EasySave.Localization;

namespace EasySave.ConsoleApp;

public class InteractiveShell
{
    private readonly BackupService _service;
    private readonly ILocalizationService _loc;

    public InteractiveShell(BackupService service, ILocalizationService loc)
    {
        _service = service;
        _loc = loc;
    }

    public async Task Run()
    {
        while (true)
        {
            ShowMenu();
            var input = Console.ReadLine();

            switch (input)
            {
                case "1": ListJobs(); break;
                case "2": CreateJob(); break;
                case "3": EditJob(); break;
                case "4": DeleteJob(); break;
                case "5": await RunJob(); break;
                case "6": await RunAll(); break;
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
        var name = Console.ReadLine();

        Console.Write(_loc.Get("prompt_source"));
        var source = Console.ReadLine();

        Console.Write(_loc.Get("prompt_target"));
        var target = Console.ReadLine();

        Console.Write(_loc.Get("prompt_type"));
        var type = Console.ReadLine() == "2"
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
            int index = int.Parse(Console.ReadLine()) - 1;

            var jobs = _service.GetJobs().ToList();
            var job = jobs[index];

            Console.Write($"{_loc.Get("prompt_name")} ({job.Name}): ");
            var name = Console.ReadLine();

            if (!string.IsNullOrWhiteSpace(name))
                job.Name = name;

            _service.UpdateJob(index, job);
        }
        catch (ArgumentOutOfRangeException)
        {
            Console.WriteLine(_loc.Get("error_invalid_index"));
        }
        catch (FormatException)
        {
            Console.WriteLine(_loc.Get("error_invalid_input"));
        }
    }

    private void DeleteJob()
    {
        try
        {
            ListJobs();

            Console.Write(_loc.Get("prompt_select"));
            int index = int.Parse(Console.ReadLine()) - 1;

            _service.RemoveJob(index);
        }
        catch (ArgumentOutOfRangeException)
        {
            Console.WriteLine(_loc.Get("error_invalid_index"));
        }
        catch (FormatException)
        {
            Console.WriteLine(_loc.Get("error_invalid_input"));
        }
    }

    private async Task RunJob()
    {
        try
        {
            ListJobs();

            Console.Write(_loc.Get("prompt_select"));
            int index = int.Parse(Console.ReadLine()) - 1;

            await _service.RunJob(index);
        }
        catch (ArgumentOutOfRangeException)
        {
            Console.WriteLine(_loc.Get("error_invalid_index"));
        }
        catch (FormatException)
        {
            Console.WriteLine(_loc.Get("error_invalid_input"));
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
        catch (FormatException)
        {
            Console.WriteLine(_loc.Get("error_invalid_input"));
        }
    }
}