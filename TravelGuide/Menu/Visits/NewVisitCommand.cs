using System;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;

namespace TravelGuide.Menu.Visits;

public sealed class NewVisitCommand : CliMenuCommand
{
    public NewVisitCommand() : base("New Visit", EMenuAction.Reload)
    {
    }

    protected override ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        //ახალი ვიზიტის შეყვანის იმპლემენტაცია მოგვიანებით გაკეთდება
        Console.WriteLine("New Visit is not implemented yet");
        return ValueTask.FromResult(true);
    }
}
