using System.Collections.Generic;

namespace TravelGuide.Runners;

public sealed class PlaceExtractResult
{
    public string? Name { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public string? Region { get; init; }
    public string? Municipality { get; init; }
    public List<string> Categories { get; init; } = [];
    public List<string> Tags { get; init; } = [];
    public string? BestSeason { get; init; }
    public List<string> Distances { get; init; } = [];
    public string? Description { get; init; }
}
