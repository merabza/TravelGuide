using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters;
using AppCliTools.CliParameters.Cruders;
using AppCliTools.LibDataInput;
using SystemTools.SystemToolsShared;
using TravelGuideDbModels;
using TravelGuideRepoInterfaces;

namespace TravelGuide.Menu.Visits;

//ერთი ვიზიტის სურათების სია. ჩანაწერის გასაღები თვითონ ფაილის სახელია, ამიტომ ცალკე
//სახელის ველი არ სჭირდება და ველების რედაქტორებიც არ აქვს — ახალი ჩანაწერი მხოლოდ
//ფოლდერიდან ფაილის არჩევით ჩნდება
public sealed class VisitImageCruder : Cruder
{
    private readonly string? _imagesFolderPath;
    private readonly ITravelGuideRepository _travelGuideRepository;
    private readonly int _visitId;

    // ReSharper disable once ConvertToPrimaryConstructor
    public VisitImageCruder(ITravelGuideRepository travelGuideRepository, int visitId, string? imagesFolderPath) : base(
        "Visit Image", "Visit Images", false, false)
    {
        _travelGuideRepository = travelGuideRepository;
        _visitId = visitId;
        _imagesFolderPath = imagesFolderPath;
    }

    protected override Dictionary<string, ItemData> GetCrudersDictionary()
    {
        try
        {
            //ბაზაში სახელები რეგისტრის გაუთვალისწინებლად ერთმანეთისგან არ განსხვავდება,
            //ამიტომ ლექსიკონიც ასეთივე შედარებით იგება — თორემ ContainsRecordWithKey
            //არარსებულ ჩანაწერს დაინახავდა და ჩამატება უნიკალურ ინდექსზე ჩავარდებოდა
            return _travelGuideRepository.GetVisitImages(_visitId).ToDictionary(k => k.FileName,
                ItemData (v) => new TextItemData { Text = v.FileName }, StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception e)
        {
            //ბაზასთან დაკავშირება ვერ მოხერხდა — მენიუ ცარიელი სიით აეწყობა
            StShared.WriteException(e, true);
            return [];
        }
    }

    protected override ItemData CreateNewItem(string? recordKey, ItemData? defaultItemData)
    {
        return new TextItemData();
    }

    public override void FillDetailsSubMenu(CliMenuSet itemSubMenuSet, string itemName)
    {
        //განზრახ ცარიელია: ჩანაწერის ქვემენიუში მხოლოდ წაშლა უნდა იყოს, საბაზო რეალიზაცია კი სახელის
        //თავისუფალი ტექსტით შეცვლის პუნქტს ამატებს, რითაც არარსებული ფაილის სახელის ჩაწერა შეიძლებოდა
    }

    public override bool ContainsRecordWithKey(string recordKey)
    {
        return GetCrudersDictionary().ContainsKey(recordKey);
    }

    protected override ValueTask AddRecordWithKey(string recordKey, ItemData newRecord,
        CancellationToken cancellationToken = default)
    {
        _travelGuideRepository.AddVisitImage(_visitId, recordKey);
        _travelGuideRepository.SaveChanges();
        return ValueTask.CompletedTask;
    }

    protected override ValueTask RemoveRecordWithKey(string recordKey, CancellationToken cancellationToken = default)
    {
        VisitImage visitImage = _travelGuideRepository.GetVisitImage(_visitId, recordKey) ??
                                throw new InvalidOperationException($"Visit Image with name {recordKey} not found");
        _travelGuideRepository.DeleteVisitImage(visitImage);

        _travelGuideRepository.SaveChanges();
        return ValueTask.CompletedTask;
    }

    protected override ValueTask<string?> InputNewRecordName(CancellationToken cancellationToken = default)
    {
        try
        {
            //უკვე მიბმული ფაილები ასარჩევ სიაში აღარ ჩანს
            return ValueTask.FromResult<string?>(ImageFileSelector.SelectFileName(_imagesFolderPath,
                [.. GetCrudersDictionary().Keys]));
        }
        catch (DataInputEscapeException)
        {
            return ValueTask.FromResult<string?>(null);
        }
    }
}
