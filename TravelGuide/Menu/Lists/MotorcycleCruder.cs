using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliParameters.Cruders;
using AppCliTools.CliParameters.FieldEditors;
using SystemTools.SystemToolsShared;
using TravelGuideDbModels;
using TravelGuideRepoInterfaces;

namespace TravelGuide.Menu.Lists;

//მოტოციკლების სიის რედაქტორი. ჩანაწერები ინახება ბაზის Motorcycles ცხრილში, ჩანაწერის სახელი Key ველია
public sealed class MotorcycleCruder : Cruder
{
    private readonly ITravelGuideRepository _travelGuideRepository;

    // ReSharper disable once ConvertToPrimaryConstructor
    public MotorcycleCruder(ITravelGuideRepository travelGuideRepository) : base("Motorcycle", "Motorcycles")
    {
        _travelGuideRepository = travelGuideRepository;
        FieldEditors.Add(new OptionalTextFieldEditor(nameof(MotorcycleModel.Manufacturer), true));
        FieldEditors.Add(new OptionalTextFieldEditor(nameof(MotorcycleModel.Model), true));
        FieldEditors.Add(new IntFieldEditor(nameof(MotorcycleModel.ReleaseYear), 0, true));
        FieldEditors.Add(new OptionalTextFieldEditor(nameof(MotorcycleModel.StateNumber), true));
    }

    private List<MotorcycleModel> GetMotorcycles()
    {
        try
        {
            return _travelGuideRepository.GetMotorcyclesList();
        }
        catch (Exception e)
        {
            //ბაზასთან დაკავშირება ვერ მოხერხდა — მენიუ ცარიელი სიით აეწყობა
            StShared.WriteException(e, true);
            return [];
        }
    }

    protected override Dictionary<string, ItemData> GetCrudersDictionary()
    {
        List<MotorcycleModel> motorcyclesList = GetMotorcycles();
        return motorcyclesList.ToDictionary(k => k.Key, ItemData (v) => v);
    }

    public override bool ContainsRecordWithKey(string recordKey)
    {
        Dictionary<string, ItemData> dict = GetCrudersDictionary();
        return dict.ContainsKey(recordKey);
    }

    protected override ItemData CreateNewItem(string? recordKey, ItemData? defaultItemData)
    {
        return new MotorcycleModel { Key = recordKey ?? string.Empty };
    }

    protected override ValueTask AddRecordWithKey(string recordKey, ItemData newRecord,
        CancellationToken cancellationToken = default)
    {
        if (newRecord is not MotorcycleModel newMotorcycle)
        {
            return ValueTask.CompletedTask;
        }

        //ყოველთვის ახალი ობიექტი ემატება: სახელის გადარქმევისას (წაშლა+დამატება) მოსული ობიექტი ძველი იდენტიფიკატორითაა
        _travelGuideRepository.CreateMotorcycle(new MotorcycleModel
        {
            Key = recordKey,
            Manufacturer = newMotorcycle.Manufacturer,
            Model = newMotorcycle.Model,
            ReleaseYear = newMotorcycle.ReleaseYear,
            StateNumber = newMotorcycle.StateNumber
        });

        _travelGuideRepository.SaveChanges();
        return ValueTask.CompletedTask;
    }

    public override ValueTask UpdateRecordWithKey(string recordKey, ItemData newRecord,
        CancellationToken cancellationToken = default)
    {
        if (newRecord is not MotorcycleModel newMotorcycle)
        {
            return ValueTask.CompletedTask;
        }

        MotorcycleModel motorcycle = _travelGuideRepository.GetMotorcycleByKey(recordKey) ??
                                     throw new InvalidOperationException($"Motorcycle with key {recordKey} not found");
        motorcycle.Manufacturer = newMotorcycle.Manufacturer;
        motorcycle.Model = newMotorcycle.Model;
        motorcycle.ReleaseYear = newMotorcycle.ReleaseYear;
        motorcycle.StateNumber = newMotorcycle.StateNumber;
        _travelGuideRepository.UpdateMotorcycle(motorcycle);

        _travelGuideRepository.SaveChanges();
        return ValueTask.CompletedTask;
    }

    protected override ValueTask RemoveRecordWithKey(string recordKey, CancellationToken cancellationToken = default)
    {
        MotorcycleModel motorcycle = _travelGuideRepository.GetMotorcycleByKey(recordKey) ??
                                     throw new InvalidOperationException($"Motorcycle with key {recordKey} not found");
        _travelGuideRepository.DeleteMotorcycle(motorcycle);

        _travelGuideRepository.SaveChanges();
        return ValueTask.CompletedTask;
    }
}
