using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliParameters.FieldEditors;
using AppCliTools.LibDataInput;
using TravelGuide.Runners;

namespace TravelGuide.Menu.Lists;

//არასავალდებულო მდებარეობის (LocationItem ან null) ველის რედაქტორი. განედი და გრძედი ერთ ველად, ერთად
//იკითხება — ცალკე ველებად ერთის შეცვლის შემდეგ ჩანაწერის მენიუ ბაზიდან თავიდან იტვირთება და ნახევრად
//შეყვანილი წყვილი დაიკარგებოდა. ცარიელი განედი (Enter მდებარეობის გარეშე, არსებულზე — Delete და დადასტურება)
//მდებარეობას ხსნის; წყვილი გეოგრაფიულ ზღვრებში უნდა იყოს — იმავე შემოწმებით, რომლითაც ქროულერი გვერდიდან
//წაკითხულს ფილტრავს
public sealed class OptionalLocationFieldEditor : FieldEditor<LocationItem?>
{
    private const string NoLocationStatus = "No Location";

    public OptionalLocationFieldEditor(string propertyName, bool enterFieldDataOnCreate = false) : base(propertyName,
        enterFieldDataOnCreate)
    {
    }

    public override ValueTask UpdateField(string? recordKey, object recordForUpdate,
        CancellationToken cancellationToken = default)
    {
        LocationItem? current = GetValue(recordForUpdate);
        double? latitude = current?.Latitude;
        double? longitude = current?.Longitude;
        while (true)
        {
            latitude = InputCoordinate("Latitude", latitude);
            if (latitude is null)
            {
                SetValue(recordForUpdate, null);
                return ValueTask.CompletedTask;
            }

            //განედის შემდეგ გრძედი სავალდებულოა
            longitude = InputCoordinate("Longitude", longitude);
            while (longitude is null)
            {
                Console.WriteLine("Longitude is required");
                longitude = InputCoordinate("Longitude", null);
            }

            if (PlaceDataExtractor.IsValidCoordinatePair(latitude.Value, longitude.Value))
            {
                SetValue(recordForUpdate, new LocationItem { Latitude = latitude.Value, Longitude = longitude.Value });
                return ValueTask.CompletedTask;
            }

            //შეყვანილი მნიშვნელობები ნაგულისხმევად რჩება, რომ მხოლოდ არასწორის გასწორება დასჭირდეს
            Console.WriteLine(
                "Invalid coordinates: latitude must be within [-90, 90] and longitude within [-180, 180]");
        }
    }

    public override string GetValueStatus(object? record)
    {
        return GetValue(record)?.GetItemKey() ?? NoLocationStatus;
    }

    //რიცხვი, ან null ცარიელი პასუხისას; რიცხვად ვერწაკითხული ტექსტი თავიდან იკითხება
    private static double? InputCoordinate(string fieldName, double? currentValue)
    {
        string? defaultText = currentValue?.ToString(CultureInfo.InvariantCulture);
        while (true)
        {
            string? input = Inputer.InputText(fieldName, defaultText);
            if (string.IsNullOrWhiteSpace(input))
            {
                return null;
            }

            if (double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
            {
                return value;
            }

            Console.WriteLine($"Invalid value for {fieldName}. Enter a number like 41.715137 (decimal separator is .)");
        }
    }
}
