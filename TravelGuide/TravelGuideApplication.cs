using AppCliTools.CliTools.Models;
using Microsoft.Extensions.Options;
using SystemTools.SystemToolsShared;

namespace TravelGuide;

public class TravelGuideApplication : IApplication
{
    public TravelGuideApplication(IOptions<ApplicationOptions> options)
    {
        AppName = options.Value.AppName;
    }

    public string AppName { get; }
}
