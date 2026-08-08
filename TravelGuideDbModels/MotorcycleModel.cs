using SystemTools.SystemToolsShared;

namespace TravelGuideDbModels;

public sealed class MotorcycleModel : ItemData
{
    public int MotorcycleId { get; init; }
    public required string Key { get; set; }
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public int ReleaseYear { get; set; }
    public string? StateNumber { get; set; }
}
