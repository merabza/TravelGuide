using System;
using System.Globalization;
using System.Text;
using ImageMagick;

namespace TravelGuide.Menu.Visits;

//სურათის კონსოლში დამხატავი. Windows Terminal-ში (WT_SESSION გარემოს ცვლადი) sixel-გრაფიკით
//ნამდვილი რასტრული სურათი იხატება; სხვა კონსოლებში — ნახევარბლოკებით, სადაც თითო სიმბოლო
//ვერტიკალურად ორ პიქსელს იჭერს: ზედა ტექსტის ფერით იხატება, ქვედა — ფონისით
internal static class ConsoleImageRenderer
{
    private const char Esc = '\u001b';
    private const char UpperHalfBlock = '▀';

    public static void Render(string imageFullPath)
    {
        // ReSharper disable once using
        // ReSharper disable once DisposableConstructor
        using var image = new MagickImage(imageFullPath);
        //ტელეფონის ფოტოებს მოტრიალება ხშირად EXIF-ში აქვთ ჩაწერილი — ჯერ ნამდვილ ორიენტაციაზე მოდის
        image.AutoOrient();

        if (Environment.GetEnvironmentVariable("WT_SESSION") is null)
        {
            RenderWithHalfBlocks(image);
        }
        else
        {
            RenderWithSixel(image);
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

    private static void RenderWithHalfBlocks(MagickImage image)
    {
        //conhost კონსოლი ANSI მიმდევრობებს მხოლოდ ცალკე ჩართვის შემდეგ იგებს; Windows Terminal-ს არ სჭირდება,
        //მაგრამ ზედმეტი ჩართვა არაფერს აფუჭებს
        TryEnableVirtualTerminal();

        ShrinkToFit(image, (uint)Math.Max(Console.WindowWidth - 1, 10),
            (uint)(Math.Max(Console.WindowHeight - 4, 5) * 2));

        // ReSharper disable once using
        using IPixelCollection<byte> pixels = image.GetPixels();
        var builder = new StringBuilder();
        int width = (int)image.Width;
        int height = (int)image.Height;
        for (int y = 0; y < height; y += 2)
        {
            for (int x = 0; x < width; x++)
            {
                IMagickColor<byte>? top = pixels.GetPixel(x, y).ToColor();
                //კენტი სიმაღლისას ბოლო სტრიქონის ქვედა ნახევარი შავად რჩება
                IMagickColor<byte>? bottom = y + 1 < height ? pixels.GetPixel(x, y + 1).ToColor() : null;
                builder.Append(CultureInfo.InvariantCulture,
                    $"{Esc}[38;2;{top?.R ?? 0};{top?.G ?? 0};{top?.B ?? 0};48;2;{bottom?.R ?? 0};{bottom?.G ?? 0};{bottom?.B ?? 0}m");
                builder.Append(UpperHalfBlock);
            }

            builder.Append(Esc).Append("[0m\n");
        }

        Console.Out.Write(builder.ToString());
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
