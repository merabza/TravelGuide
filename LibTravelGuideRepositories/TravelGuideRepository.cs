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
            throw;
        }
    }

    public int SaveChangesWithTransaction()
    {
        try
        {
            // ReSharper disable once using
            using var transaction = GetTransaction();
            try
            {
                var ret = _context.SaveChanges();
                transaction.Commit();
                return ret;
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Error occurred executing {nameof(SaveChangesWithTransaction)}.");
            throw;
        }
    }

    public IDbContextTransaction GetTransaction()
    {
        return _context.Database.BeginTransaction();
    }
}