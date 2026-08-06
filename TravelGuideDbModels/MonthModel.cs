namespace TravelGuideDbModels;

public sealed class MonthModel
{
    //MonthId კალენდარული თვის ნომერია (1..12) და კოდიდან ენიჭება
    public int MonthId { get; init; }
    public required string Name { get; init; }
}
