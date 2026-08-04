namespace TravelGuide.Models;

public sealed class PlaceModel
{
    public required string Url { get; set; }
    public string? HeaderText { get; set; }
    public string? Location { get; set; }
    public EState State { get; set; }
}
