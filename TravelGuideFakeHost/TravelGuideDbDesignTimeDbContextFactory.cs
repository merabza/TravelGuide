//Created by FakeProjectDesignTimeDbContextFactoryCreator at 8/2/2025 5:16:00 PM

using Microsoft.EntityFrameworkCore;
using SystemTools.DatabaseToolsShared;
using TravelGuideDbMigration;
using TravelGuideDbPersistence;

namespace TravelGuideFakeHost;

//ეს კლასი საჭიროა იმისათვის, რომ შესაძლებელი გახდეს მიგრაციასთან მუშაობა.
//ანუ დეველოპერ ბაზის წაშლა და ახლიდან დაგენერირება, ან მიგრაციაში ცვლილებების გაკეთება
// ReSharper disable once UnusedType.Global
public sealed class TravelGuideDbDesignTimeDbContextFactory : SqlServerDesignTimeDbContextFactory<TravelGuideDbContext>
{
    //DataProvider მოდის FakeHost პროექტის appsettings.json ფაილიდან,
    //ხოლო ConnectionStringSeed, როგორც დაცული ინფორმაცია, FakeHost-ის User Secrets-იდან (UserSecretsId წერია FakeHost.csproj-ში).
    //კონსტრუქტორი აუცილებლად უპარამეტრო უნდა იყოს, რადგან dotnet ef ამ კლასს თვითონ ქმნის რეფლექსიით
    // ReSharper disable once ConvertToPrimaryConstructor
    public TravelGuideDbDesignTimeDbContextFactory() : base(AssemblyReference.Assembly.GetName().Name!,
        "ConnectionStringSeed", true)
    {
    }

    protected override TravelGuideDbContext CreateDbContext(DbContextOptions<TravelGuideDbContext> options)
    {
        // ReSharper disable once DisposableConstructor
        return new TravelGuideDbContext(options);
    }
}
