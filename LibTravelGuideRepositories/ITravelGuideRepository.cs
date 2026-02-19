//Created by RepositoryInterfaceCreator at 7/24/2025 11:44:10 PM

using Microsoft.EntityFrameworkCore.Storage;

namespace LibTravelGuideRepositories;

public interface ITravelGuideRepository
{
    int SaveChanges();
    int SaveChangesWithTransaction();
    IDbContextTransaction GetTransaction();
}
