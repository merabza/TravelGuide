//Created by RepositoryCreatorFactoryCreator at 7/24/2025 11:44:10 PM

using Microsoft.Extensions.DependencyInjection;
using TravelGuideRepoInterfaces;

namespace TravelGuideRepositories;

public sealed class TravelGuideRepositoryCreatorFactory : ITravelGuideRepositoryCreatorFactory
{
    private readonly IServiceProvider _services;

    public TravelGuideRepositoryCreatorFactory(IServiceProvider services)
    {
        _services = services;
    }

    public ITravelGuideRepository GetTravelGuideRepository()
    {
        // ReSharper disable once using
        using IServiceScope scope = _services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<ITravelGuideRepository>();
    }
}
