using SystemTools.SystemToolsShared;

namespace TravelGuide.Menu.Lists;

//ცნობარის (რეგიონები, მუნიციპალიტეტები, კატეგორიები, ტეგები, საწყისი წერტილები) რედაქტორის ჩანაწერი —
//ბაზის ჩანაწერის მოუბმელი ასლი, რომელსაც სახელის რედაქტორი ცვლის. Id ბაზის ჩანაწერის იდენტიფიკატორია,
//რომლითაც შენახვისას ბმული ჩანაწერი მოიძებნება (სახელი კი შეიძლება უკვე შეცვლილი იყოს); ახალ ჩანაწერს
//ის ჯერ არ აქვს (0)
public sealed class LookupItem : ItemData
{
    public LookupItem(int id, string name)
    {
        Id = id;
        Name = name;
    }

    public int Id { get; }
    public string Name { get; set; }

    //გასაღები სახელია — ის ცნობარში უნიკალურია
    public override string GetItemKey()
    {
        return Name;
    }
}
