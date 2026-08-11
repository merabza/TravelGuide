using System;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;

namespace TravelGuide.Menu.Visits;

public sealed class RecommendedVisitsCommand : CliMenuCommand
{
    public RecommendedVisitsCommand() : base("Recommended Visits", EMenuAction.Reload)
    {
    }

    protected override ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        //რეკომენდებული ვიზიტების სიის იმპლემენტაცია მოგვიანებით გაკეთდება
        Console.WriteLine("Recommended Visits is not implemented yet");
        return ValueTask.FromResult(true);
    }
}
