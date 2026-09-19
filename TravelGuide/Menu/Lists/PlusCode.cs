using System;

namespace TravelGuide.Menu.Lists;

//Google-ის Plus Code-ის (Open Location Code) გაშიფვრა კოორდინატებად — ადგილის დასახელებაში ჩაწერილი კოდისთვის
//(FindLocationByNameCommand). სრული კოდი (გამყოფამდე 8 სიმბოლო, მაგალითად „8HH6RPQM+8C“) პირდაპირ იშიფრება; მოკლეს
//(თავიდან 2, 4 ან 6 სიმბოლო აკლია — Google Maps ასეთს აჩვენებს: „RPQM+8C მცხეთა“) საყრდენი წერტილი სჭირდება, რომლის
//მიდამოშიც აღდგება (RecoverNearest). ალგორითმები სპეციფიკაციისაა (github.com/google/open-location-code):
//კოდის ციფრი ანბანში სიმბოლოს ინდექსია, პირველი წყვილი (განედი, გრძედი) 20°-იან უჯრას ნიშნავს, ყოველი შემდეგი —
//20-ჯერ უფრო წვრილს (1°, 0.05°, 0.0025°, 0.000125°), მეთერთმეტედან ბადის ციფრები უჯრას 5 რიგად და 4 სვეტად ყოფს;
//შედეგი უჯრის ცენტრია
public static class PlusCode
{
    //ანბანი — 20 სიმბოლო, ხმოვნებისა და ერთმანეთის მსგავსი ასოების გარეშე
    private const string Alphabet = "23456789CFGHJMPQRVWX";
    private const int EncodingBase = 20;
    private const char Separator = '+';
    private const int SeparatorPosition = 8;
    private const int PairCodeLength = 10;
    private const int MaxDigitCount = 15;
    private const int GridRows = 5;
    private const int GridColumns = 4;
    private const double LatitudeMax = 90;
    private const double LongitudeMax = 180;

    //წყვილების სიზუსტე: ბოლო (მეხუთე) წყვილის უჯრა 1/8000 გრადუსია; პირველი წყვილის ერთეული — 20⁴ ასეთი უჯრა (20°)
    private const long PairPrecision = 8000;
    private const long PairFirstPlaceValue = EncodingBase * EncodingBase * EncodingBase * EncodingBase;

    //ბადის სიზუსტე ხუთი ბადის ციფრით: განედი 8000·5⁵, გრძედი 8000·4⁵; პირველი ბადის ციფრის ერთეული — 5⁴ და 4⁴
    private const long GridRowsTotal = GridRows * GridRows * GridRows * GridRows * GridRows;
    private const long GridColumnsTotal = GridColumns * GridColumns * GridColumns * GridColumns * GridColumns;
    private const long FinalLatitudePrecision = PairPrecision * GridRowsTotal;
    private const long FinalLongitudePrecision = PairPrecision * GridColumnsTotal;
    private const long GridLatitudeFirstPlaceValue = GridRowsTotal / GridRows;
    private const long GridLongitudeFirstPlaceValue = GridColumnsTotal / GridColumns;

    //სრული კოდია — გამყოფი მერვე სიმბოლოს შემდეგაა
    public static bool IsFull(string code)
    {
        return code.IndexOf(Separator) == SeparatorPosition;
    }

    //სრული კოდის უჯრის ცენტრი. მნიშვნელობები მთელ რიცხვებად ითვლება და ბოლოს იყოფა, რომ წილადის ცდომილება არ დაგროვდეს
    public static (double Latitude, double Longitude) Decode(string code)
    {
        string digits = code.Replace(Separator.ToString(), string.Empty, StringComparison.Ordinal).ToUpperInvariant();
        if (digits.Length > MaxDigitCount)
        {
            digits = digits[..MaxDigitCount];
        }

        //წყვილები: პირველი 20°-იან უჯრას ნიშნავს, ყოველი შემდეგი 20-ჯერ უფრო წვრილს
        var latitudeValue = (long)(-LatitudeMax * PairPrecision);
        var longitudeValue = (long)(-LongitudeMax * PairPrecision);
        int pairDigitsCount = Math.Min(digits.Length, PairCodeLength);
        long placeValue = PairFirstPlaceValue;
        for (var i = 0; i < pairDigitsCount; i += 2)
        {
            latitudeValue += DigitValue(digits[i]) * placeValue;
            longitudeValue += DigitValue(digits[i + 1]) * placeValue;
            if (i < pairDigitsCount - 2)
            {
                placeValue /= EncodingBase;
            }
        }

        double latitudePrecision = (double)placeValue / PairPrecision;
        double longitudePrecision = (double)placeValue / PairPrecision;

        //ბადის ციფრები: თითოეული უჯრას 5 რიგად და 4 სვეტად ყოფს
        long gridLatitudeValue = 0;
        long gridLongitudeValue = 0;
        if (digits.Length > PairCodeLength)
        {
            long rowPlaceValue = GridLatitudeFirstPlaceValue;
            long columnPlaceValue = GridLongitudeFirstPlaceValue;
            for (int i = PairCodeLength; i < digits.Length; i++)
            {
                int digitValue = DigitValue(digits[i]);
                gridLatitudeValue += digitValue / GridColumns * rowPlaceValue;
                gridLongitudeValue += digitValue % GridColumns * columnPlaceValue;
                if (i < digits.Length - 1)
                {
                    rowPlaceValue /= GridRows;
                    columnPlaceValue /= GridColumns;
                }
            }

            latitudePrecision = (double)rowPlaceValue / FinalLatitudePrecision;
            longitudePrecision = (double)columnPlaceValue / FinalLongitudePrecision;
        }

        double latitude = (double)latitudeValue / PairPrecision + (double)gridLatitudeValue / FinalLatitudePrecision;
        double longitude = (double)longitudeValue / PairPrecision +
                           (double)gridLongitudeValue / FinalLongitudePrecision;
        return (Math.Min(latitude + latitudePrecision / 2, LatitudeMax), longitude + longitudePrecision / 2);
    }

    //მოკლე კოდის აღდგენა საყრდენი წერტილის მიდამოში — აბრუნებს უჯრის ცენტრს. კოდს თავიდან (8 − გამყოფის პოზიცია)
    //სიმბოლო აკლია, ამიტომ უჯრის ზომა 20^(2 − აკლია/2) გრადუსია (4 სიმბოლოს დაკლებისას 1°); საყრდენის კოდის თავი
    //ემატება და, თუ შედეგი საყრდენს ნახევარ უჯრაზე მეტით სცილდება, ერთი უჯრით საყრდენისკენ იწევს
    public static (double Latitude, double Longitude) RecoverNearest(string shortCode, double referenceLatitude,
        double referenceLongitude)
    {
        int paddingLength = SeparatorPosition - shortCode.IndexOf(Separator);
        double resolution = Math.Pow(EncodingBase, 2 - paddingLength / 2.0);
        double halfResolution = resolution / 2;

        (double latitude, double longitude) =
            Decode(EncodePairs(referenceLatitude, referenceLongitude)[..paddingLength] + shortCode);

        if (referenceLatitude + halfResolution < latitude && latitude - resolution >= -LatitudeMax)
        {
            latitude -= resolution;
        }
        else if (referenceLatitude - halfResolution > latitude && latitude + resolution <= LatitudeMax)
        {
            latitude += resolution;
        }

        if (referenceLongitude + halfResolution < longitude)
        {
            longitude -= resolution;
        }
        else if (referenceLongitude - halfResolution > longitude)
        {
            longitude += resolution;
        }

        return (latitude, longitude);
    }

    //კოორდინატების კოდირება 10 წყვილ-ციფრად (გამყოფის გარეშე) — მოკლე კოდის აღსადგენად მხოლოდ თავი სჭირდება.
    //მნიშვნელობა ბოლო წყვილის უჯრებში ითვლება და ციფრები ბოლოდან იწერება
    private static string EncodePairs(double latitude, double longitude)
    {
        //ჩრდილოეთ პოლუსი ბოლო უჯრაში რომ მოხვდეს და არა მის იქით
        if (latitude >= LatitudeMax)
        {
            latitude = LatitudeMax - 1.0 / PairPrecision;
        }

        long latitudeValue = (long)Math.Floor(Math.Round((latitude + LatitudeMax) * FinalLatitudePrecision, 6)) /
                             GridRowsTotal;
        long longitudeValue =
            (long)Math.Floor(Math.Round((longitude + LongitudeMax) * FinalLongitudePrecision, 6)) / GridColumnsTotal;
        var code = new char[PairCodeLength];
        for (int i = PairCodeLength - 2; i >= 0; i -= 2)
        {
            code[i] = Alphabet[(int)(latitudeValue % EncodingBase)];
            code[i + 1] = Alphabet[(int)(longitudeValue % EncodingBase)];
            latitudeValue /= EncodingBase;
            longitudeValue /= EncodingBase;
        }

        return new string(code);
    }

    private static int DigitValue(char digit)
    {
        int value = Alphabet.IndexOf(digit);
        return value >= 0 ? value : throw new FormatException($"Invalid Plus code character {digit}");
    }
}
