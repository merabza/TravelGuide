using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliParameters.Cruders;
using AppCliTools.CliParameters.FieldEditors;
using SystemTools.SystemToolsShared;
using TravelGuideRepoInterfaces;

namespace TravelGuide.Menu.Lists;

//ცნობარის ცხრილის (Regions, Municipalities, Categories, Tags, FromPoints) რედაქტორის საერთო ნაწილი.
//ჩანაწერს მხოლოდ სახელი აქვს და ისვე გასაღებია (fieldKeyFromItem=true — ცალკე Record Name ველი არ
//სჭირდება). ცნობარის ჩანაწერს ადგილები იდენტიფიკატორით ეყრდნობა, ამიტომ სახელის შეცვლა ჩანაწერის
//წაშლა-ხელახლა შექმნა კი არა, ადგილზე გადარქმევაა (UpdateRecordWithKey) — ეს „Edit All fields in
//sequence" ბრძანებით და ჩანაწერის მენიუს Name ველითაც კეთდება — ჩარჩოს ველის რედაქტორი გასაღების ცვლილებას
//ხედავს (Cruder.CheckRecordKeyChanged): სიის მენიუს თავიდან აწყობინებს და წარმატებისას ერთი დონით ზევით
//ბრუნდება, რადგან ჩანაწერის მენიუ ძველი სახელით უსარგებლოა.
//გამოყენებული ჩანაწერი არ იშლება — რეგიონი/მუნიციპალიტეტი ადგილების რედაქტორით გადაება შეიძლება,
//დანარჩენი ბმულები (კატეგორიები, ტეგები, მანძილები) მხოლოდ ხელახალი ქროულინგით იცვლება
public abstract class LookupCruder : Cruder
{
    private readonly int _nameMaxLength;

    protected LookupCruder(ITravelGuideRepository travelGuideRepository, string crudName, string crudNamePlural,
        int nameMaxLength) : base(crudName, crudNamePlural, true)
    {
        TravelGuideRepository = travelGuideRepository;
        _nameMaxLength = nameMaxLength;
        FieldEditors.Add(new TextFieldEditor(nameof(LookupItem.Name)));
    }

    protected ITravelGuideRepository TravelGuideRepository { get; }

    //ცნობარის ყველა ჩანაწერი
    protected abstract List<LookupItem> LoadItems();

    //ამ სახელის ჩანაწერის იდენტიფიკატორი ან null — სახელი ბაზის შედარებით მოწმდება, რომ უნიკალურ ინდექსს
    //დაემთხვეს
    protected abstract int? FindIdByName(string name);

    protected abstract void Create(string name);

    protected abstract void Rename(int id, string name);

    //რამდენი ადგილი ეყრდნობა ჩანაწერს
    protected abstract int GetUsageCount(int id);

    protected abstract void Delete(int id);

    protected override Dictionary<string, ItemData> GetCrudersDictionary()
    {
        try
        {
            //სახელი ბაზაში უნიკალურია, ამიტომ გასაღებები არ მეორდება
            return LoadItems().ToDictionary(k => k.Name, ItemData (v) => v, StringComparer.Ordinal);
        }
        catch (Exception e)
        {
            //ბაზასთან დაკავშირება ვერ მოხერხდა — მენიუ ცარიელი სიით აეწყობა
            StShared.WriteException(e, true);
            return [];
        }
    }

    public override bool ContainsRecordWithKey(string recordKey)
    {
        return GetCrudersDictionary().ContainsKey(recordKey);
    }

    protected override ItemData CreateNewItem(string? recordKey, ItemData? defaultItemData)
    {
        return new LookupItem(0, string.Empty);
    }

    public override bool CheckValidation(ItemData item)
    {
        return item is LookupItem lookupItem && TryGetValidName(lookupItem, out _);
    }

    protected override ValueTask AddRecordWithKey(string recordKey, ItemData newRecord,
        CancellationToken cancellationToken = default)
    {
        //recordKey აქ შემთხვევითი Guid-ია (fieldKeyFromItem) — ჩანაწერის გასაღები სახელია
        if (newRecord is not LookupItem newItem || !TryGetValidName(newItem, out string name))
        {
            return ValueTask.CompletedTask;
        }

        Create(name);

        TravelGuideRepository.SaveChanges();
        return ValueTask.CompletedTask;
    }

    public override ValueTask UpdateRecordWithKey(string recordKey, ItemData newRecord,
        CancellationToken cancellationToken = default)
    {
        //სახელი არ შეცვლილა — გასაღები იგივე დარჩა და შესანახი არაფერია
        if (newRecord is not LookupItem newItem || !TryGetValidName(newItem, out string name) || name == recordKey)
        {
            return ValueTask.CompletedTask;
        }

        Rename(newItem.Id, name);

        TravelGuideRepository.SaveChanges();
        return ValueTask.CompletedTask;
    }

    protected override ValueTask RemoveRecordWithKey(string recordKey, CancellationToken cancellationToken = default)
    {
        if (!GetCrudersDictionary().TryGetValue(recordKey, out ItemData? itemData) ||
            itemData is not LookupItem item)
        {
            throw new InvalidOperationException($"{CrudName} with key {recordKey} not found");
        }

        //ადგილები ჩანაწერს იდენტიფიკატორით ეყრდნობა (Places.RegionId, PlacesByCategories და სხვ.) —
        //გამოყენებულის წაშლა ან ბაზაში ჩავარდებოდა, ან ბმულებს კასკადით წაშლიდა, ამიტომ ის უარიყოფა
        int usageCount = GetUsageCount(item.Id);
        if (usageCount > 0)
        {
            StShared.WriteErrorLine($"{CrudName} {recordKey} is used by {usageCount} places and cannot be deleted",
                true);
            return ValueTask.CompletedTask;
        }

        Delete(item.Id);

        TravelGuideRepository.SaveChanges();
        return ValueTask.CompletedTask;
    }

    //სახელი ზედმეტი ჰარების გარეშე ინახება; ცარიელი, ზედმეტად გრძელი ან სხვა ჩანაწერის მიერ დაკავებული
    //სახელი შეცდომას წერს
    private bool TryGetValidName(LookupItem item, out string name)
    {
        name = item.Name.Trim();
        if (name.Length == 0)
        {
            StShared.WriteErrorLine($"{CrudName} name is required", true);
            return false;
        }

        if (name.Length > _nameMaxLength)
        {
            StShared.WriteErrorLine($"{CrudName} name is too long (max {_nameMaxLength} characters)", true);
            return false;
        }

        int? existingId = FindIdByName(name);
        if (existingId is null || existingId == item.Id)
        {
            return true;
        }

        StShared.WriteErrorLine($"{CrudName} with name {name} already exists", true);
        return false;
    }
}
