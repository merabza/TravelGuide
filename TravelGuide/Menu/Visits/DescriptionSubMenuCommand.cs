using System;
using System.Text;
using AppCliTools.CliMenu;

namespace TravelGuide.Menu.Visits;

//აღწერის პუნქტი: მშობელ მენიუში ტექსტი ერთ სტრიქონად ჩანს სტატუსად, ნომრის აკრეფისას კი
//იხსნება ქვემენიუ, სადაც სრული ტექსტი კონსოლის სიგანეზე დაკეცილი სტრიქონებით გამოდის
//და Escape წინა მენიუში აბრუნებს
public sealed class DescriptionSubMenuCommand : CliMenuCommand
{
    private readonly string _description;
    private readonly string _oneLineStatus;

    // ReSharper disable once ConvertToPrimaryConstructor
    public DescriptionSubMenuCommand(string description) : base("Description", EMenuAction.LoadSubMenu)
    {
        _description = description;
        //სტატუსი ერთ სტრიქონად — ახალი ხაზები მენიუს პუნქტს დაშლიდა, ამიტომ ჰარეები იკეცება
        _oneLineStatus = string.Join(' ', description.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    protected override string? GetStatus()
    {
        return _oneLineStatus;
    }

    public override CliMenuSet GetSubMenu()
    {
        var descriptionMenuSet = new CliMenuSet("Description");

        //სტრიქონები მენიუს პუნქტების სახით გამოდის, ამიტომ სიგრძე კონსოლის სიგანეს უნდა ჩაეტიოს:
        //6 სიმბოლო პუნქტის ნომერს მიაქვს და კიდევ 1 იმისთვის, რომ ზუსტად სავსე სტრიქონი თავისით არ გადავიდეს ახალ ხაზზე
        int maxLineLength = Math.Max(20, Console.WindowWidth - 7);
        foreach (string paragraph in _description.Split('\n'))
        {
            AppendWrappedParagraph(descriptionMenuSet, paragraph, maxLineLength);
        }

        descriptionMenuSet.AddEscapeCommand("Exit to Place menu");
        return descriptionMenuSet;
    }

    //აბზაცს სიტყვების საზღვრებზე კეცავს და თითო სტრიქონს თითო პუნქტად ამატებს.
    //პუნქტის არჩევა არაფერს აკეთებს (EMenuAction.Nothing), ტექსტი მხოლოდ საჩვენებელია
    private static void AppendWrappedParagraph(CliMenuSet menuSet, string paragraph, int maxLineLength)
    {
        var currentLine = new StringBuilder();
        foreach (string word in paragraph.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            string rest = word;
            //მთელ სტრიქონზე გრძელი სიტყვა ნაწილებად იჭრება
            while (rest.Length > maxLineLength)
            {
                if (currentLine.Length > 0)
                {
                    AddTextLine(menuSet, currentLine.ToString());
                    currentLine.Clear();
                }

                AddTextLine(menuSet, rest[..maxLineLength]);
                rest = rest[maxLineLength..];
            }

            if (rest.Length == 0)
            {
                continue;
            }

            if (currentLine.Length == 0)
            {
                currentLine.Append(rest);
                continue;
            }

            if (currentLine.Length + 1 + rest.Length <= maxLineLength)
            {
                currentLine.Append(' ').Append(rest);
                continue;
            }

            AddTextLine(menuSet, currentLine.ToString());
            currentLine.Clear();
            currentLine.Append(rest);
        }

        if (currentLine.Length > 0)
        {
            AddTextLine(menuSet, currentLine.ToString());
        }
    }

    private static void AddTextLine(CliMenuSet menuSet, string line)
    {
        //nameIsStatus სტრიქონს მნიშვნელობის ფერით (ცისფრად) ხატავს, როგორც სხვა მენიუებში სტატუსები იხატება
        menuSet.AddMenuItem(new CliMenuCommand(line, nameIsStatus: true));
    }
}
