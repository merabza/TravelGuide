using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using TravelGuideDbModels;
using TravelGuideRepoInterfaces;

namespace TravelGuide.Menu.Visits;

// ReSharper disable once ConvertToPrimaryConstructor
public sealed class LastVisitsCommand : CliMenuCommand
{
    //ბოლო ვიზიტების მაქსიმალური რაოდენობა
    private const int LastVisitsCount = 10;

    private readonly ITravelGuideRepositoryCreatorFactory _travelGuideRepositoryCreatorFactory;

    public LastVisitsCommand(ITravelGuideRepositoryCreatorFactory travelGuideRepositoryCreatorFactory) : base(
        "Last Visits", EMenuAction.Reload)
    {
        _travelGuideRepositoryCreatorFactory = travelGuideRepositoryCreatorFactory;
    }

    protected override ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        MenuAction = EMenuAction.Reload;

        ITravelGuideRepository repository = _travelGuideRepositoryCreatorFactory.GetTravelGuideRepository();

        //ბაზიდან ამოირჩევა ბოლო ვიზიტები თარიღის კლებადობით
        List<VisitListItem> lastVisits = repository.GetLastVisits(LastVisitsCount);

        if (lastVisits.Count == 0)
        {
            Console.WriteLine("No Visits found");
            return ValueTask.FromResult(true);
        }

        Console.WriteLine($"Last {lastVisits.Count} Visits:");

        //თითო ვიზიტი თითო სტრიქონზე: თარიღი, ადგილი და მოტოციკლი
        foreach (VisitListItem visit in lastVisits)
        {
            Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
                $"{visit.VisitDate:yyyy-MM-dd} | {visit.PlaceName} | {visit.MotorcycleKey}"));
        }

        return ValueTask.FromResult(true);
    }
}
