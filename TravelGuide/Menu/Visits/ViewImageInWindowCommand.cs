using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using SystemTools.SystemToolsShared;

namespace TravelGuide.Menu.Visits;

//სურათის გახსნა ცალკე ფანჯარაში — გაფართოებაზე მიბმული ნაგულისხმევი პროგრამით (Windows-ზე ჩვეულებრივ Photos)
public sealed class ViewImageInWindowCommand : CliMenuCommand
{
    private readonly string _imageFullPath;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ViewImageInWindowCommand(string imageFullPath) : base("View Image (Window)", EMenuAction.Reload)
    {
        _imageFullPath = imageFullPath;
    }

    protected override ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        MenuAction = EMenuAction.Reload;

        if (!File.Exists(_imageFullPath))
        {
            StShared.WriteErrorLine($"Image file {_imageFullPath} does not exists", true);
            return ValueTask.FromResult(false);
        }

        //UseShellExecute ფაილს ისე ხსნის, როგორც Explorer-ში ორმაგი წკაპი — მიბმული პროგრამით.
        //დაბრუნებული Process მხოლოდ განთავისუფლდება, გახსნილი ფანჯარა ცალკე ცხოვრობს
        // ReSharper disable once using
        using Process? process = Process.Start(new ProcessStartInfo(_imageFullPath) { UseShellExecute = true });
        return ValueTask.FromResult(true);
    }
}
