using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliParameters.Cruders;
using AppCliTools.CliParameters.FieldEditors;
using ParametersManagement.LibParameters;
using SystemTools.SystemToolsShared;
using TravelGuide.FieldEditors;
using TravelGuideDbModels;
using TravelGuideRepoInterfaces;

namespace TravelGuide.Menu.Visits;

//ერთი ადგილის ვიზიტების რედაქტორი My Places-ის ყაიდაზე: ვიზიტის მენიუში ველები მიმდინარე
//მნიშვნელობებით ჩანს და შეიძლება როგორც თითო ველის, ისე ყველა ველის თანმიმდევრობით შეცვლა.
//ჩანაწერის გასაღები წარწერაა (თარიღი + მოტოციკლი) და რედაქტირებისას თვითონაც იცვლება —
//ამიტომ გასაღები ყოველ გამოყენებამდე ვიზიტის იდენტიფიკატორით თავიდან დგინდება
public sealed class VisitCruder : Cruder
{
    private readonly int _placeId;
    private readonly ITravelGuideRepository _travelGuideRepository;

    //fieldKeyFromItem=true — ვიზიტს რედაქტირებადი სახელი არ აქვს და Record Name ველი არ სჭირდება
    public VisitCruder(ITravelGuideRepository travelGuideRepository, int placeId,
        IParametersManager parametersManager) : base("Visit", "Visits", true)
    {
        _travelGuideRepository = travelGuideRepository;
        _placeId = placeId;
        FieldEditors.Add(new DateFieldEditor(nameof(VisitModel.VisitDate), DateTime.Today, true));
        FieldEditors.Add(new MotorcycleIdFieldEditor(nameof(VisitModel.MotorcycleId), travelGuideRepository, true));
        FieldEditors.Add(new OptionalTextFieldEditor(nameof(VisitModel.Comment), true));
        FieldEditors.Add(new VisitImagesFieldEditor(nameof(VisitModel.Images), travelGuideRepository,
            parametersManager));
    }

    //ერთი ადგილის ვიზიტები წარწერა-გასაღებებით თარიღის კლებადობით. ვიზიტს ნავიგაციები არ აქვს,
    //ამიტომ წარწერისთვის მოტოციკლის სახელები ცალკე მოიპოვება; გამეორებული წარწერა რიგითი ნომრით
    //განსხვავდება — CliMenuSet.GetMenuItemWithName SingleOrDefault-ს იყენებს და გამეორებული სახელი
    //ბოლო ბრძანების გამეორებისას გამონაკლისს ისვრის
    public List<KeyValuePair<string, VisitModel>> GetKeyedVisits()
    {
        try
        {
            List<VisitModel> visits = _travelGuideRepository.GetVisitsByPlaceId(_placeId);
            Dictionary<int, string> motorcycleKeys =
                _travelGuideRepository.GetMotorcyclesList().ToDictionary(k => k.MotorcycleId, v => v.MotorcycleKey);

            var captionCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            List<KeyValuePair<string, VisitModel>> keyedVisits = [];
            foreach (VisitModel visit in visits)
            {
                string caption = string.Create(CultureInfo.InvariantCulture,
                    $"{visit.VisitDate:yyyy-MM-dd} | {motorcycleKeys[visit.MotorcycleId]}");
                int seenCount = captionCounts.GetValueOrDefault(caption);
                captionCounts[caption] = seenCount + 1;
                if (seenCount > 0)
                {
                    caption = string.Create(CultureInfo.InvariantCulture, $"{caption} ({seenCount + 1})");
                }

                keyedVisits.Add(new KeyValuePair<string, VisitModel>(caption, visit));
            }

            return keyedVisits;
        }
        catch (Exception e)
        {
            //ბაზასთან დაკავშირება ვერ მოხერხდა — მენიუ ცარიელი სიით აეწყობა
            StShared.WriteException(e, true);
            return [];
        }
    }

    //რედაქტირების შემდეგ ძველი წარწერა-გასაღები აღარ არსებობს — ვიზიტის იდენტიფიკატორით ახალი დგინდება
    public string? GetRecordKeyByVisitId(int visitId)
    {
        return GetKeyedVisits().FirstOrDefault(f => f.Value.VisitId == visitId).Key;
    }

    protected override Dictionary<string, ItemData> GetCrudersDictionary()
    {
        return GetKeyedVisits().ToDictionary(k => k.Key, ItemData (v) => v.Value);
    }

    public override bool ContainsRecordWithKey(string recordKey)
    {
        Dictionary<string, ItemData> dict = GetCrudersDictionary();
        return dict.ContainsKey(recordKey);
    }

    public override ValueTask UpdateRecordWithKey(string recordKey, ItemData newRecord,
        CancellationToken cancellationToken = default)
    {
        if (newRecord is not VisitModel newVisit)
        {
            return ValueTask.CompletedTask;
        }

        //რედაქტორები მოუბმელ ასლს ცვლიან; შესანახად ბმული ჩანაწერი იდენტიფიკატორით მოიძებნება
        VisitModel visit = _travelGuideRepository.GetVisitById(newVisit.VisitId) ??
                           throw new InvalidOperationException($"Visit with id {newVisit.VisitId} not found");
        visit.VisitDate = newVisit.VisitDate;
        visit.MotorcycleId = newVisit.MotorcycleId;
        visit.Comment = newVisit.Comment;
        _travelGuideRepository.UpdateVisit(visit);

        _travelGuideRepository.SaveChanges();
        return ValueTask.CompletedTask;
    }

    protected override ValueTask RemoveRecordWithKey(string recordKey, CancellationToken cancellationToken = default)
    {
        Dictionary<string, ItemData> dict = GetCrudersDictionary();
        if (!dict.TryGetValue(recordKey, out ItemData? itemData) || itemData is not VisitModel visitCopy)
        {
            throw new InvalidOperationException($"Visit with key {recordKey} not found");
        }

        VisitModel visit = _travelGuideRepository.GetVisitById(visitCopy.VisitId) ??
                           throw new InvalidOperationException($"Visit with id {visitCopy.VisitId} not found");
        _travelGuideRepository.DeleteVisit(visit);

        _travelGuideRepository.SaveChanges();
        return ValueTask.CompletedTask;
    }
}
