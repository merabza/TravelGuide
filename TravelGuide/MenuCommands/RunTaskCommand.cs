using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.LibDataInput;
using OpenQA.Selenium.Chrome;
using SystemTools.SystemToolsShared;
using TravelGuide.Runners;
using TravelGuideDbModels;
using TravelGuideRepoInterfaces;

namespace TravelGuide.MenuCommands;

// ReSharper disable once ConvertToPrimaryConstructor
public sealed class RunTaskCommand : CliMenuCommand
{
    private readonly string _taskName;
    private readonly ITravelGuideRepositoryCreatorFactory _travelGuideRepositoryCreatorFactory;

    public RunTaskCommand(ITravelGuideRepositoryCreatorFactory travelGuideRepositoryCreatorFactory, string taskName) :
        base("Run this task", EMenuAction.Reload)
    {
        _travelGuideRepositoryCreatorFactory = travelGuideRepositoryCreatorFactory;
        _taskName = taskName;
    }

    protected override ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        MenuAction = EMenuAction.Reload;

        //ამოცანის გაშვების პროცესი დაიწყო
        Console.WriteLine($"Run Task {_taskName} started");

        ITravelGuideRepository repository = _travelGuideRepositoryCreatorFactory.GetTravelGuideRepository();

        //მოვძებნოთ ამოცანა ბაზაში
        TaskModel? task = repository.GetTaskByName(_taskName);
        if (task is null)
        {
            StShared.WriteErrorLine($"Task with Name {_taskName} not found", true);
            return ValueTask.FromResult(false);
        }

        //ამოცანას საწყისი წერტილების გარეშე გაშვება არ შეუძლია
        if (task.StartPoints.Count == 0)
        {
            StShared.WriteErrorLine($"Task {_taskName} does not have Start Points", true);
            return ValueTask.FromResult(false);
        }

        //თუ ბაზაში უკვე გაანალიზებული ადგილებია, ვკითხოთ მომხმარებელს, ხელახლა დამუშავდეს თუ არა
        var reProcessAnalysed = false;
        if (repository.HasAnalysedPlaces())
        {
            reProcessAnalysed = Inputer.InputBool("Re-process already analysed places?", false, false);
        }

        //თითოეული საწყისი წერტილისთვის გაეშვას პროცესი ცალკე ბრაუზერით
        foreach (TaskStartPoint startPoint in task.StartPoints.OrderBy(o => o.StartPoint, StringComparer.Ordinal))
        {
            Console.WriteLine($"Run process for Start Point {startPoint.StartPoint}");

            //Chrome-ის შიდა ლოგები (GCM და მსგავსი ხმაური) კონსოლში რომ არ გამოვიდეს
            var chromeOptions = new ChromeOptions();
            chromeOptions.AddExcludedArgument("enable-logging");
            // ReSharper disable once using
            // ReSharper disable once DisposableConstructor
            using var driver = new ChromeDriver(chromeOptions);
            var runner = new GeorgianTravelGuideRunner(driver, startPoint.StartPoint, repository, reProcessAnalysed);

            //ხელახლა დამუშავება მხოლოდ პირველი საწყისი წერტილის ფარგლებში სრულდება
            reProcessAnalysed = false;
            bool success = runner.Run();
            driver.Quit();

            if (success)
            {
                continue;
            }

            StShared.WriteErrorLine($"Process failed for Start Point {startPoint.StartPoint}", true);
            return ValueTask.FromResult(false);
        }

        //ამოცანის გაშვების პროცესი დასრულდა
        Console.WriteLine($"Run Task {_taskName} Finished");
        return ValueTask.FromResult(true);
    }
}
