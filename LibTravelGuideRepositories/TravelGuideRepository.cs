//Created by RepositoryClassCreator at 7/24/2025 11:44:10 PM

using System;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using TravelGuideDb;

namespace LibTravelGuideRepositories;

public sealed class TravelGuideRepository : ITravelGuideRepository
{
    private readonly TravelGuideDbContext _context;
    private readonly ILogger<TravelGuideRepository> _logger;

    public TravelGuideRepository(TravelGuideDbContext ctx, ILogger<TravelGuideRepository> logger)
    {
        _context = ctx;
        _logger = logger;
    }

    public int SaveChanges()
    {
        try
        {
            return _context.SaveChanges();
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Error occurred executing {nameof(SaveChanges)}.");
            throw new InvalidOperationException("Failed to save changes to the database.", e);
        }
    }

    public int SaveChangesWithTransaction()
    {
        try
        {
            // ReSharper disable once using
            using IDbContextTransaction transaction = GetTransaction();
            try
            {
                int ret = _context.SaveChanges();
                transaction.Commit();
                return ret;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new InvalidOperationException(
                    "Failed to save changes within transaction. Transaction rolled back.", ex);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Error occurred executing {nameof(SaveChangesWithTransaction)}.");
            throw new InvalidOperationException($"Failed to execute {nameof(SaveChangesWithTransaction)}.", e);
        }
    }

    public IDbContextTransaction GetTransaction()
    {
        return _context.Database.BeginTransaction();
    }
}
