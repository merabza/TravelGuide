using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
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

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
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
            return false;
        }

        //ამოცანას საწყისი წერტილების გარეშე გაშვება არ შეუძლია
        if (task.StartPoints.Count == 0)
        {
            StShared.WriteErrorLine($"Task {_taskName} does not have Start Points", true);
            return false;
        }

        //ბმულების შეგროვების გზას მომხმარებელი ირჩევს: sitemap-ის მოქაჩვა (სწრაფია და ბრაუზერს არ საჭიროებს)
        //თუ Selenium-ით სიის გვერდის ჩამოსქროლვა
        bool useSitemap = Inputer.InputBool("Use sitemap for URL harvesting (No = Selenium listing walk)?", true,
            false);

        //თუ ბაზაში უკვე გაანალიზებული ადგილებია, ვკითხოთ მომხმარებელს, ხელახლა დამუშავდეს თუ არა
        var reProcessAnalysed = false;
        if (repository.HasAnalysedPlaces())
        {
            reProcessAnalysed = Inputer.InputBool("Re-process already analysed places?", false, false);
        }

        //გვერდები ბრაუზერის გარეშე, პირდაპირ HTTP-ით მოიქაჩება — Chrome მხოლოდ Selenium-რეჟიმის სიის გვერდს სჭირდება
        using var httpClientHandler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            CheckCertificateRevocationList = true
        };
        using var httpClient = new HttpClient(httpClientHandler, disposeHandler: false);
        httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("TravelGuideBot", "1.0"));

        if (useSitemap)
        {
            //sitemap მთელ საიტს ფარავს, ამიტომ საწყისი წერტილებიდან მხოლოდ პირველის ჰოსტი გამოიყენება და ერთხელ ეშვება
            string firstStartPoint = task.StartPoints.OrderBy(o => o.StartPoint, StringComparer.Ordinal).First()
                .StartPoint;
            var harvester = new SitemapUrlHarvester(httpClient, firstStartPoint, repository);
            if (!await harvester.RunAsync(cancellationToken).ConfigureAwait(false))
            {
                StShared.WriteErrorLine("Sitemap harvesting failed", true);
                return false;
            }
        }
        else
        {
            //თითოეული საწყისი წერტილისთვის ბმულების შეგროვება ცალკე ბრაუზერით ეშვება
            foreach (TaskStartPoint startPoint in task.StartPoints.OrderBy(o => o.StartPoint, StringComparer.Ordinal))
            {
                Console.WriteLine($"Run process for Start Point {startPoint.StartPoint}");

                //Chrome-ის შიდა ლოგები (GCM და მსგავსი ხმაური) კონსოლში რომ არ გამოვიდეს
                var chromeOptions = new ChromeOptions();
                chromeOptions.AddExcludedArgument("enable-logging");
                // ReSharper disable once using
                // ReSharper disable once DisposableConstructor
                using var driver = new ChromeDriver(chromeOptions);
                var runner = new GeorgianTravelGuideRunner(driver, startPoint.StartPoint, repository);
                bool success = runner.Run();
                driver.Quit();

                if (success)
                {
                    continue;
                }

                StShared.WriteErrorLine($"Process failed for Start Point {startPoint.StartPoint}", true);
                return false;
            }
        }

        //შეგროვებული გვერდების გაანალიზება HTTP-ით, შეგროვების რეჟიმის მიუხედავად ერთხელ სრულდება
        var analyser = new PlaceAnalyser(httpClient, repository, reProcessAnalysed);
        await analyser.RunAsync(cancellationToken).ConfigureAwait(false);

        //ამოცანის გაშვების პროცესი დასრულდა
        Console.WriteLine($"Run Task {_taskName} Finished");
        return true;
    }
}
