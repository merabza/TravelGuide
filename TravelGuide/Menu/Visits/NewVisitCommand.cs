using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters.CliMenuCommands;
using AppCliTools.LibDataInput;
using AppCliTools.LibMenuInput;
using SystemTools.SystemToolsShared;
using TravelGuideDbModels;
using TravelGuideRepoInterfaces;

namespace TravelGuide.Menu.Visits;

// ReSharper disable once ConvertToPrimaryConstructor
public sealed class NewVisitCommand : CliMenuCommand
{
    private readonly int _placeId;
    private readonly ITravelGuideRepositoryCreatorFactory _travelGuideRepositoryCreatorFactory;

    public NewVisitCommand(ITravelGuideRepositoryCreatorFactory travelGuideRepositoryCreatorFactory, int placeId) :
        base("New Visit", EMenuAction.Reload)
    {
        _travelGuideRepositoryCreatorFactory = travelGuideRepositoryCreatorFactory;
        _placeId = placeId;
    }

    protected override ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        MenuAction = EMenuAction.Reload;

        //ვიზიტის დაფიქსირების პროცესი დაიწყო
        Console.WriteLine("Create new Visit started");

        //ვიზიტის თარიღის შეყვანა (ნაგულისხმევად დღევანდელი დღე)
        DateTime visitDate = Inputer.InputDate("Visit Date", DateTime.Today);

        ITravelGuideRepository repository = _travelGuideRepositoryCreatorFactory.GetTravelGuideRepository();

        //მოტოციკლი სიიდან უნდა აირჩეს — ცარიელი სიით ვიზიტის დაფიქსირება შეუძლებელია
        List<MotorcycleModel> motorcycles =
        [
            .. repository.GetMotorcyclesList().OrderBy(o => o.MotorcycleKey, StringComparer.Ordinal)
        ];
        if (motorcycles.Count == 0)
        {
            StShared.WriteErrorLine("No Motorcycles found. Create a Motorcycle in Lists menu first", true);
            return ValueTask.FromResult(false);
        }

        //მოტოციკლების ასარჩევი სიის აგება
        var motorcyclesMenuSet = new CliMenuSet();
        foreach (MotorcycleModel motorcycle in motorcycles)
        {
            motorcyclesMenuSet.AddMenuItem(new MenuCommandWithStatusCliMenuCommand(motorcycle.MotorcycleKey));
        }

        int selectedId = MenuInputer.InputIdFromMenuList("Motorcycle", motorcyclesMenuSet);
        if (selectedId < 0 || selectedId >= motorcycles.Count)
        {
            StShared.WriteErrorLine("Selected invalid Motorcycle", true);
            return ValueTask.FromResult(false);
        }

        //კომენტარი არასავალდებულოა — ცარიელი Enter მას ცარიელად ტოვებს, Escape კი მხოლოდ
        //კომენტარზე ამბობს უარს და უკვე შეყვანილ თარიღსა და მოტოციკლს არ კარგავს
        string? comment;
        try
        {
            comment = Inputer.InputText("Comment", null);
        }
        catch (DataInputEscapeException)
        {
            comment = null;
        }

        //ვიზიტის ჩანაწერის შექმნა და ბაზაში შენახვა
        repository.CreateVisit(new VisitModel
        {
            PlaceId = _placeId,
            MotorcycleId = motorcycles[selectedId].MotorcycleId,
            VisitDate = visitDate,
            Comment = comment
        });
        repository.SaveChanges();

        //ვიზიტის დაფიქსირების პროცესი დასრულდა
        Console.WriteLine("Create new Visit Finished");
        return ValueTask.FromResult(true);
    }
}
