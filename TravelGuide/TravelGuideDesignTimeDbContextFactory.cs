//Created by ProjectDesignTimeDbContextFactoryClassCreator at 7/24/2025 11:44:10 PM

using TravelGuideDb;

namespace TravelGuide;

//ეს კლასი საჭიროა იმისათვის, რომ შესაძლებელი გახდეს მიგრაციასთან მუშაობა.
//ანუ დეველოპერ ბაზის წაშლა და ახლიდან დაგენერირება, ან მიგრაციაში ცვლილებების გაკეთება
// ReSharper disable once UnusedType.Global
public sealed class TravelGuideDesignTimeDbContextFactory : DesignTimeDbContextFactory<TravelGuideDbContext>
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public TravelGuideDesignTimeDbContextFactory() : base("TravelGuideDbMigration", "ConnectionString",
        @"D:\1WorkSecurity\TravelGuide\TravelGuide.json")
    {
    }
}