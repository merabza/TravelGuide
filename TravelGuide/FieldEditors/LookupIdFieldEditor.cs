using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters.FieldEditors;
using AppCliTools.LibDataInput;
using AppCliTools.LibMenuInput;
using SystemTools.SystemToolsShared;

namespace TravelGuide.FieldEditors;

//ცნობარიდან ასარჩევი არასავალდებულო ველის რედაქტორი (რეგიონი, მუნიციპალიტეტი): მნიშვნელობად ცნობარის
//ჩანაწერის იდენტიფიკატორი ინახება, ეკრანზე მისი სახელი ჩანს და არჩევა ცნობარის სიიდან ხდება;
//„(None)" მნიშვნელობას ასუფთავებს
public sealed class LookupIdFieldEditor : FieldEditor<int?>
{
    private const string NoneCaption = "(None)";
    private readonly Func<Dictionary<int, string>> _getLookupItems;

    //propertyDescriptor — მენიუში ველი Region Id-ის ნაცვლად Region წარწერით გამოდის
    public LookupIdFieldEditor(string propertyName, string propertyDescriptor,
        Func<Dictionary<int, string>> getLookupItems, bool enterFieldDataOnCreate = false) : base(propertyName,
        enterFieldDataOnCreate, null, false, propertyDescriptor)
    {
        _getLookupItems = getLookupItems;
    }

    public override ValueTask UpdateField(string? recordKey, object recordForUpdate,
        CancellationToken cancellationToken = default)
    {
        Dictionary<int, string> lookupItems = _getLookupItems();
        List<KeyValuePair<int, string>> orderedItems = [.. lookupItems.OrderBy(o => o.Value, StringComparer.Ordinal)];

        //ასარჩევი სია: „(None)" პუნქტი „-" კლავიშზეა, დანარჩენს რიგითი იდენტიფიკატორი ცალსახად ენიჭება,
        //რომ Enter-ით ნაგულისხმევის (მიმდინარე მნიშვნელობის) არჩევამაც სწორი ინდექსი დააბრუნოს
        var lookupMenuSet = new CliMenuSet();
        lookupMenuSet.AddMenuItem("-", new CliMenuCommand(NoneCaption));
        for (var i = 0; i < orderedItems.Count; i++)
        {
            lookupMenuSet.AddMenuItem(new CliMenuCommand(orderedItems[i].Value), i);
        }

        int? currentId = GetValue(recordForUpdate);
        string currentName = currentId is int id && lookupItems.TryGetValue(id, out string? name) ? name : NoneCaption;
        int selectedId = MenuInputer.InputIdFromMenuList(FieldName, lookupMenuSet, currentName);
        if (selectedId == -1)
        {
            SetValue(recordForUpdate, null);
            return ValueTask.CompletedTask;
        }

        if (selectedId < 0 || selectedId >= orderedItems.Count)
        {
            throw new DataInputException($"Selected invalid {FieldName}");
        }

        SetValue(recordForUpdate, orderedItems[selectedId].Key);
        return ValueTask.CompletedTask;
    }

    public override string GetValueStatus(object? record)
    {
        try
        {
            int? currentId = GetValue(record);
            return currentId is int id && _getLookupItems().TryGetValue(id, out string? name) ? name : string.Empty;
        }
        catch (Exception e)
        {
            //სტატუსი მენიუს ხატვისას გამონაკლისების დამუშავების გარეშე ითხოვება
            StShared.WriteException(e, true);
            return string.Empty;
        }
    }
}
