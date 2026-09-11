// ReSharper disable CollectionNeverUpdated.Global

using SystemTools.SystemToolsShared;

namespace TravelGuideDbModels;

//ვიზიტის ჩანაწერი: რომელი ლოკაცია, რომელი მოტოციკლით და რომელ თარიღში.
//ვიზიტი ლოკაციაზეა და არა ადგილზე — მრავალლოკაციიანი ადგილის ერთი ლოკაციის მონახულება დანარჩენებს
//ნამყოფად არ ნიშნავს.
//ItemData საჭიროა VisitCruder-ისთვის — ჩანაწერი ველების რედაქტორებით იმართება
public sealed class VisitModel : ItemData
{
    public int VisitId { get; init; }
    public int LocationId { get; init; }
    public int MotorcycleId { get; set; }
    public DateTime VisitDate { get; set; }
    public string? Comment { get; set; }

    //სურათების სია ცალკე კრუდერით იმართება — ველების რედაქტორი მას მხოლოდ კითხულობს
    public ICollection<VisitImage> Images { get; init; } = new HashSet<VisitImage>();
}
