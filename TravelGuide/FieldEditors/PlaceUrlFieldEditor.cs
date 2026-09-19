using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliParameters.FieldEditors;
using AppCliTools.LibDataInput;
using SystemTools.SystemToolsShared;
using TravelGuideCore.Domain;
using TravelGuideCore.Domain.PlaceModels;
using TravelGuideCore.Domain.UrlModels;
using TravelGuideDbPart.Db.Configurations;
using TravelGuideRepoInterfaces;

namespace TravelGuide.FieldEditors;

//ადგილის მისამართის ველის რედაქტორი: მისამართი ადგილის ველი კი არა, მისი Urls ცხრილის ჩანაწერია (UrlNavigation),
//ამიტომ რედაქტორი საბაზისო კლასის რეფლექსიით ველის ძებნას არ იყენებს. ახალ ადგილს მისამართი არ ეკითხება
//(enterFieldDataOnCreate=false — არც შექმნისას, არც თანმიმდევრობით რედაქტირებისას), ის ჩანაწერის მენიუდან,
//მოგვიანებით, ერთხელ იწერება — მხოლოდ უმისამართო ადგილს (PlaceCruder.CheckFieldsEnables), რადგან მიწერილი
//მისამართი აღარ იცვლება: ქროულერი მისამართს სწორედ ხეშ-კოდით ცნობს. ცარიელი Enter ადგილს უმისამართოდ ტოვებს.
//მითითებულ მისამართს ბოლო დახრილი ხაზი ეჭრება და უნიკალურობა Urls ცხრილში ხეშ-კოდით მოწმდება ისევე, როგორც
//ქროულერის HarvestedUrlPersister-ში. ახალი მისამართის სტატუსი Analysed-ია, რომ ხელით შეყვანილ ადგილს ქროულერი
//არ შეეხოს; Urls-ის ჩანაწერს ბაზაში PlaceCruder.UpdateRecordWithKey ადგილთან ერთად ინახავს
public sealed class PlaceUrlFieldEditor : FieldEditor
{
    private readonly ITravelGuideRepository _travelGuideRepository;

    public PlaceUrlFieldEditor(string propertyName, ITravelGuideRepository travelGuideRepository) : base(
        propertyName, null, false)
    {
        _travelGuideRepository = travelGuideRepository;
    }

    public override ValueTask UpdateField(string? recordKey, object recordForUpdate,
        CancellationToken cancellationToken = default)
    {
        if (recordForUpdate is not PlaceModel { UrlNavigation: null } place)
        {
            return ValueTask.CompletedTask;
        }

        while (true)
        {
            string? url = Inputer.InputText(FieldName, null)?.Trim().TrimEnd('/');
            if (string.IsNullOrEmpty(url))
            {
                return ValueTask.CompletedTask;
            }

            if (url.Length > UrlModelConfiguration.UrlLength)
            {
                StShared.WriteErrorLine($"Url is too long (max {UrlModelConfiguration.UrlLength} characters)", true,
                    null, false);
                continue;
            }

            int urlHashCode = url.GetDeterministicHashCode();
            if (_travelGuideRepository.GetUrlIdsByUrlHashCode(urlHashCode).ContainsKey(url))
            {
                StShared.WriteErrorLine($"Url {url} already exists", true, null, false);
                continue;
            }

            place.UrlNavigation = new UrlModel { Url = url, UrlHashCode = urlHashCode, State = EState.Analysed };
            return ValueTask.CompletedTask;
        }
    }

    public override string GetValueStatus(object? record)
    {
        return record is PlaceModel { UrlNavigation.Url: { } url } ? url : string.Empty;
    }
}
