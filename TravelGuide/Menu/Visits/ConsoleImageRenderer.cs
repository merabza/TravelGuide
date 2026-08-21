using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using ImageMagick;

namespace TravelGuide.Menu.Visits;

//სურათის კონსოლში დამხატავი. თუ ტერმინალი sixel-გრაფიკას უჭერს მხარს (Windows Terminal და
//Windows 11-ის ახალი conhost-იც), ნამდვილი რასტრული სურათი იხატება; სხვაგან — კვადრანტული
//ბლოკებით, სადაც თითო სიმბოლო 2×2 ქვეპიქსელს იჭერს: ნახევარბლოკებზე ორჯერ წვრილი ბადეა
internal static class ConsoleImageRenderer
{
    private const char Esc = '\u001b';

    //16 კვადრანტული სიმბოლო ბიტებით ინდექსირებული: 1=ზედა-მარცხენა, 2=ზედა-მარჯვენა,
    //4=ქვედა-მარცხენა, 8=ქვედა-მარჯვენა
    private const string QuadrantGlyphs = " ▘▝▀▖▌▞▛▗▚▐▜▄▙▟█";

    //ტერმინალის შესაძლებლობების გარკვევა პასუხის ლოდინს მოითხოვს, ამიტომ შედეგი მხოლოდ ერთხელ დგინდება
    private static bool? _terminalSupportsSixel;

    public static void Render(string imageFullPath)
    {
        // ReSharper disable once using
        // ReSharper disable once DisposableConstructor
        using var image = new MagickImage(imageFullPath);
        //ტელეფონის ფოტოებს მოტრიალება ხშირად EXIF-ში აქვთ ჩაწერილი — ჯერ ნამდვილ ორიენტაციაზე მოდის
        image.AutoOrient();

        //conhost კონსოლი ANSI მიმდევრობებს (მათ შორის ქვემოთ გაგზავნილ DA1 შეკითხვას) მხოლოდ ცალკე
        //ჩართვის შემდეგ იგებს; Windows Terminal-ს არ სჭირდება, მაგრამ ზედმეტი ჩართვა არაფერს აფუჭებს
        TryEnableVirtualTerminal();

        _terminalSupportsSixel ??= DetectSixelSupport();

        if (_terminalSupportsSixel == true)
        {
            RenderWithSixel(image);
        }
        else
        {
            RenderWithQuadrants(image);
        }
    }

    //WT_SESSION მხოლოდ Windows Terminal-ის საკუთარ ჩანართებში ჩანს. Windows 11-ზე conhost-ის
    //ფანჯრებსაც (მაგალითად Visual Studio-დან გაშვებისას) ხშირად Windows Terminal ხატავს ამ ცვლადის
    //გარეშე, ამიტომ დამატებით თავად ტერმინალს ეკითხება Primary Device Attributes (DA1) შეკითხვით —
    //პასუხის ატრიბუტი „4" sixel-გრაფიკის მხარდაჭერას ნიშნავს
    private static bool DetectSixelSupport()
    {
        if (Environment.GetEnvironmentVariable("WT_SESSION") is not null)
        {
            return true;
        }

        if (Console.IsInputRedirected || Console.IsOutputRedirected)
        {
            return false;
        }

        try
        {
            //შეკითხვამდე შემთხვევით დაგროვილი კლავიშები იწმინდება, პასუხში რომ არ აირიოს
            while (Console.KeyAvailable)
            {
                Console.ReadKey(true);
            }

            Console.Out.Write(Esc);
            Console.Out.Write("[c");

            //პასუხი ჩვეულებრივ მყისიერად მოდის; ხანგრძლივი დუმილი ნიშნავს, რომ ტერმინალი DA1-ს ვერ იგებს
            var response = new StringBuilder();
            long deadline = Environment.TickCount64 + 500;
            while (Environment.TickCount64 < deadline)
            {
                if (!Console.KeyAvailable)
                {
                    Thread.Sleep(10);
                    continue;
                }

                char keyChar = Console.ReadKey(true).KeyChar;
                response.Append(keyChar);
                if (keyChar == 'c')
                {
                    break;
                }
            }

            //დაგვიანებული ან ნახევრად მოსული პასუხი მენიუმ კლავიშებად არ უნდა მიიღოს
            while (Console.KeyAvailable)
            {
                Console.ReadKey(true);
            }

            //პასუხის ფორმაა ESC[?61;4;...c — ატრიბუტები „?"-სა და ბოლო „c"-ს შორის წერია
            string reply = response.ToString();
            int questionMarkIndex = reply.IndexOf('?');
            if (questionMarkIndex < 0)
            {
                return false;
            }

            int finalIndex = reply.IndexOf('c', questionMarkIndex);
            if (finalIndex < 0)
            {
                return false;
            }

            string[] attributes = reply.Substring(questionMarkIndex + 1, finalIndex - questionMarkIndex - 1)
                .Split(';');
            return Array.Exists(attributes, static attribute => attribute == "4");
        }
        catch (Exception e) when (e is InvalidOperationException or IOException)
        {
            //კონსოლის გარეშე გარემოში sixel-ზე უარის თქმა უსაფრთხო არჩევანია
            return false;
        }
    }

    //sixel-ის ზომა პიქსელებში იზომება, კონსოლის ფანჯარა კი უჯრებით. უჯრის ზომა პიქსელებში ზუსტად
    //არ ვიცით, ამიტომ ფრთხილი შეფასება (8×16) გამოიყენება, რომ სურათი ეკრანის საზღვრებს არ გასცდეს
    private static void RenderWithSixel(MagickImage image)
    {
        const int cellWidthPixels = 8;
        const int cellHeightPixels = 16;
        ShrinkToFit(image, (uint)(Math.Max(Console.WindowWidth - 2, 10) * cellWidthPixels),
            (uint)(Math.Max(Console.WindowHeight - 4, 5) * cellHeightPixels));

        //sixel-ნაკადი მთლიანად ASCII სიმბოლოებისგან შედგება
        Console.Out.Write(Encoding.ASCII.GetString(image.ToByteArray(MagickFormat.Sixel)));
        Console.Out.WriteLine();
    }

    //თითო უჯრაში კვადრანტული სიმბოლოთი 2×2 ქვეპიქსელი ეტევა, მაგრამ მხოლოდ ორი ფერი (სიმბოლოსი და
    //ფონის). ამიტომ უჯრის ოთხი ქვეპიქსელი სიკაშკაშის მიხედვით ორ ჯგუფად იყოფა და თითო ჯგუფი თავისი
    //საშუალო ფერით იხატება
    private static void RenderWithQuadrants(MagickImage image)
    {
        //უჯრის სავარაუდო ზომაა 8×16 პიქსელი, ანუ ქვეპიქსელი (4×8) სიგანეზე ორჯერ მაღალია —
        //პროპორციების შესანარჩუნებლად სურათი განზე ორმაგი სიმკვრივით იჭრება
        uint maxSamplesWidth = (uint)(Math.Max(Console.WindowWidth - 1, 10) * 2);
        uint maxSamplesHeight = (uint)(Math.Max(Console.WindowHeight - 4, 5) * 2);
        double targetRatio = 2.0 * image.Width / image.Height;

        uint samplesWidth;
        uint samplesHeight;
        if (targetRatio * maxSamplesHeight <= maxSamplesWidth)
        {
            samplesHeight = maxSamplesHeight;
            samplesWidth = Math.Max(1u, (uint)Math.Round(targetRatio * maxSamplesHeight));
        }
        else
        {
            samplesWidth = maxSamplesWidth;
            samplesHeight = Math.Max(1u, (uint)Math.Round(maxSamplesWidth / targetRatio));
        }

        //ჩარჩოზე პატარა სურათი არ დიდდება: პიქსელს მაქსიმუმ ერთი უჯრა ეთმობა, როგორც აქამდე ნახევარბლოკებისას
        if (image.Width * 2u < samplesWidth)
        {
            samplesWidth = image.Width * 2u;
            samplesHeight = image.Height;
        }

        image.Resize(new MagickGeometry(samplesWidth, samplesHeight) { IgnoreAspectRatio = true });

        // ReSharper disable once using
        using IPixelCollection<byte> pixels = image.GetPixels();
        var builder = new StringBuilder();
        int width = (int)image.Width;
        int height = (int)image.Height;
        for (int y = 0; y < height; y += 2)
        {
            for (int x = 0; x < width; x += 2)
            {
                AppendQuadrantCell(builder, pixels, x, y, width, height);
            }

            builder.Append(Esc).Append("[0m\n");
        }

        Console.Out.Write(builder.ToString());
    }

    private static void AppendQuadrantCell(StringBuilder builder, IPixelCollection<byte> pixels, int x, int y,
        int width, int height)
    {
        //კენტი განზომილების კიდეზე ქვეპიქსელი მეზობლის გამეორებით ივსება
        int nextX = Math.Min(x + 1, width - 1);
        int nextY = Math.Min(y + 1, height - 1);
        Span<int> subX = stackalloc int[] { x, nextX, x, nextX };
        Span<int> subY = stackalloc int[] { y, y, nextY, nextY };

        Span<int> red = stackalloc int[4];
        Span<int> green = stackalloc int[4];
        Span<int> blue = stackalloc int[4];
        Span<int> luminance = stackalloc int[4];
        int luminanceSum = 0;
        for (int i = 0; i < 4; i++)
        {
            IMagickColor<byte>? color = pixels.GetPixel(subX[i], subY[i]).ToColor();
            red[i] = color?.R ?? 0;
            green[i] = color?.G ?? 0;
            blue[i] = color?.B ?? 0;
            //აღქმული სიკაშკაშე ITU-R BT.601 წონებით
            luminance[i] = 299 * red[i] + 587 * green[i] + 114 * blue[i];
            luminanceSum += luminance[i];
        }

        int luminanceAverage = luminanceSum / 4;
        int bits = 0;
        int onCount = 0;
        int onRed = 0;
        int onGreen = 0;
        int onBlue = 0;
        int offCount = 0;
        int offRed = 0;
        int offGreen = 0;
        int offBlue = 0;
        for (int i = 0; i < 4; i++)
        {
            if (luminance[i] > luminanceAverage)
            {
                bits |= 1 << i;
                onCount++;
                onRed += red[i];
                onGreen += green[i];
                onBlue += blue[i];
            }
            else
            {
                offCount++;
                offRed += red[i];
                offGreen += green[i];
                offBlue += blue[i];
            }
        }

        //საშუალოზე მუქი ან ტოლი ქვეპიქსელი ყოველთვის მოიძებნება, ამიტომ offCount ნულოვანი ვერ იქნება;
        //ერთფეროვანი უჯრისას bits=0 რჩება — ცარიელი სიმბოლო მთლიანად ფონის ფერით იხატება
        int backgroundRed = offRed / offCount;
        int backgroundGreen = offGreen / offCount;
        int backgroundBlue = offBlue / offCount;
        int foregroundRed = onCount > 0 ? onRed / onCount : backgroundRed;
        int foregroundGreen = onCount > 0 ? onGreen / onCount : backgroundGreen;
        int foregroundBlue = onCount > 0 ? onBlue / onCount : backgroundBlue;

        builder.Append(CultureInfo.InvariantCulture,
            $"{Esc}[38;2;{foregroundRed};{foregroundGreen};{foregroundBlue};48;2;{backgroundRed};{backgroundGreen};{backgroundBlue}m");
        builder.Append(QuadrantGlyphs[bits]);
    }

    //ჩაატევს სურათს მოცემულ ჩარჩოში პროპორციების შენარჩუნებით; ჩარჩოზე პატარა სურათი არ დიდდება
    private static void ShrinkToFit(MagickImage image, uint maxWidth, uint maxHeight)
    {
        image.Resize(new MagickGeometry(maxWidth, maxHeight) { Greater = true });
    }

    private static void TryEnableVirtualTerminal()
    {
        nint consoleHandle = NativeMethods.GetStdHandle(NativeMethods.StdOutputHandle);
        if (NativeMethods.GetConsoleMode(consoleHandle, out uint consoleMode))
        {
            _ = NativeMethods.SetConsoleMode(consoleHandle,
                consoleMode | NativeMethods.EnableVirtualTerminalProcessing);
        }
    }
}
