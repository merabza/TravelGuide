using System;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;

namespace TravelGuide.Menu.Visits;

public sealed class LastVisitsCommand : CliMenuCommand
{
    public LastVisitsCommand() : base("Last Visits", EMenuAction.Reload)
    {
    }

    protected override ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        //ბოლო ვიზიტების სიის იმპლემენტაცია მოგვიანებით გაკეთდება
        Console.WriteLine("Last Visits is not implemented yet");
        return ValueTask.FromResult(true);
    }
}
