using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters.CliMenuCommands;
using AppCliTools.CliParameters.FieldEditors;
using AppCliTools.LibDataInput;
using AppCliTools.LibMenuInput;
using SystemTools.SystemToolsShared;
using TravelGuideDbModels;
using TravelGuideRepoInterfaces;

namespace TravelGuide.FieldEditors;

//მოტოციკლის ველის რედაქტორი: მნიშვნელობად MotorcycleId ინახება, ეკრანზე კი MotorcycleKey ჩანს
//და არჩევა მოტოციკლების სიიდან ხდება
public sealed class MotorcycleIdFieldEditor : FieldEditor<int>
{
    private readonly ITravelGuideRepository _travelGuideRepository;

    //propertyDescriptor Motorcycle — მენიუში ველი Motorcycle Id-ის ნაცვლად Motorcycle წარწერით გამოდის
    public MotorcycleIdFieldEditor(string propertyName, ITravelGuideRepository travelGuideRepository,
        bool enterFieldDataOnCreate = false) : base(propertyName, enterFieldDataOnCreate, 0, false, "Motorcycle")
    {
        _travelGuideRepository = travelGuideRepository;
    }

    public override ValueTask UpdateField(string? recordKey, object recordForUpdate,
        CancellationToken cancellationToken = default)
    {
        //მოტოციკლი სიიდან უნდა აირჩეს — ცარიელი სიით ველის შევსება შეუძლებელია
        List<MotorcycleModel> motorcycles =
        [
            .. _travelGuideRepository.GetMotorcyclesList().OrderBy(o => o.MotorcycleKey, StringComparer.Ordinal)
        ];
        if (motorcycles.Count == 0)
        {
            StShared.WriteErrorLine("No Motorcycles found. Create a Motorcycle in Lists menu first", true);
            throw new DataInputEscapeException("No Motorcycles found");
        }

        //მოტოციკლების ასარჩევი სიის აგება (ნაგულისხმევად მიმდინარე მნიშვნელობის შესაბამისი მოტოციკლი)
        var motorcyclesMenuSet = new CliMenuSet();
        foreach (MotorcycleModel motorcycle in motorcycles)
        {
            motorcyclesMenuSet.AddMenuItem(new MenuCommandWithStatusCliMenuCommand(motorcycle.MotorcycleKey));
        }

        int currentMotorcycleId = GetValue(recordForUpdate);
        string? currentMotorcycleKey =
            motorcycles.FirstOrDefault(f => f.MotorcycleId == currentMotorcycleId)?.MotorcycleKey;
        int selectedId = MenuInputer.InputIdFromMenuList(FieldName, motorcyclesMenuSet, currentMotorcycleKey);
        if (selectedId < 0 || selectedId >= motorcycles.Count)
        {
            throw new DataInputException($"Selected invalid {FieldName}");
        }

        SetValue(recordForUpdate, motorcycles[selectedId].MotorcycleId);
        return ValueTask.CompletedTask;
    }

    public override string GetValueStatus(object? record)
    {
        try
        {
            int motorcycleId = GetValue(record);
            return _travelGuideRepository.GetMotorcyclesList().FirstOrDefault(f => f.MotorcycleId == motorcycleId)
                ?.MotorcycleKey ?? string.Empty;
        }
        catch (Exception e)
        {
            //სტატუსი მენიუს ხატვისას გამონაკლისების დამუშავების გარეშე ითხოვება
            StShared.WriteException(e, true);
            return string.Empty;
        }
    }
}
