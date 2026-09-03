using System;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliParameters.FieldEditors;
using AppCliTools.LibDataInput;

namespace TravelGuide.FieldEditors;

//აღწერის ველის რედაქტორი: ტექსტი მრავალსტრიქონიანი და გრძელია, ამიტომ მენიუს სტატუსში ერთ სტრიქონად
//დაკეცილი და კონსოლის სიგანეზე შეკვეცილი ჩანს — ახალი ხაზები მენიუს პუნქტს დაშლიდა, ზედმეტად გრძელი
//სტატუსი კი ცხრილური ხედის სვეტებს ისე შეავიწროებდა, რომ ველების სახელები აღარ გამოჩნდებოდა
public sealed class DescriptionFieldEditor : FieldEditor<string?>
{
    public DescriptionFieldEditor(string propertyName, bool enterFieldDataOnCreate = false) : base(propertyName,
        enterFieldDataOnCreate)
    {
    }

    public override ValueTask UpdateField(string? recordKey, object recordForUpdate,
        CancellationToken cancellationToken = default)
    {
        //ნაგულისხმევად მიმდინარე ტექსტია — ცარიელი Enter მას უცვლელად ტოვებს
        SetValue(recordForUpdate, Inputer.InputText(FieldName, GetValue(recordForUpdate)));
        return ValueTask.CompletedTask;
    }

    public override string GetValueStatus(object? record)
    {
        string? val = GetValue(record);
        if (string.IsNullOrWhiteSpace(val))
        {
            return string.Empty;
        }

        string oneLine = string.Join(' ', val.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        //ველის სახელის სვეტს, გამყოფსა და პუნქტის ნომერს დაახლოებით 25 სიმბოლო მიაქვს
        int maxLength = Math.Max(20, Console.WindowWidth - 25);
        return oneLine.Length > maxLength ? $"{oneLine[..maxLength]}..." : oneLine;
    }
}
