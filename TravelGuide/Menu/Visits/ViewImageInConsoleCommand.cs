using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using SystemTools.SystemToolsShared;

namespace TravelGuide.Menu.Visits;

//სურათის ჩვენება პირდაპირ კონსოლში. დახატვის შემდეგ Reload-ის პაუზა სურათს ეკრანზე აჩერებს,
//სანამ მომხმარებელი რომელიმე ღილაკს არ დააჭერს — მერე მენიუ თავიდან იხატება
public sealed class ViewImageInConsoleCommand : CliMenuCommand
{
    private readonly string _imageFullPath;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ViewImageInConsoleCommand(string imageFullPath) : base("View Image (Console)", EMenuAction.Reload)
    {
        _imageFullPath = imageFullPath;
    }

    protected override ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        //გამონაკლისის შემთხვევაშიც (მაგალითად დაზიანებული ფაილი) მენიუ პაუზის შემდეგ თავიდან უნდა დაიხატოს
        MenuAction = EMenuAction.Reload;

        if (!File.Exists(_imageFullPath))
        {
            StShared.WriteErrorLine($"Image file {_imageFullPath} does not exists", true);
            return ValueTask.FromResult(false);
        }

        ConsoleImageRenderer.Render(_imageFullPath);
        return ValueTask.FromResult(true);
    }
}
