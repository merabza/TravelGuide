//Created by RepositoryClassCreator at 7/24/2025 11:44:10 PM

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using TravelGuideDbModels;
using TravelGuideDbPersistence;
using TravelGuideRepoInterfaces;

namespace TravelGuideRepositories;

public sealed class TravelGuideRepository : ITravelGuideRepository
{
    private const int MaxChangesCount = 100000;
    private readonly TravelGuideDbContext _context;
    private readonly ILogger<TravelGuideRepository> _logger;

    private int _changesCount;

    public TravelGuideRepository(TravelGuideDbContext ctx, ILogger<TravelGuideRepository> logger)
    {
        _context = ctx;
        _logger = logger;
    }

    public bool NeedSaveChanges()
    {
        return _changesCount >= MaxChangesCount;
    }

    public int SaveChanges()
    {
        _changesCount = 0;
        return _context.SaveChanges();
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

    #region Task cruder

    public List<TaskModel> GetTasksList()
    {
        return _context.Tasks.Include(i => i.StartPoints).ToList();
    }

    public TaskModel? GetTaskByName(string taskName)
    {
        return _context.Tasks.Include(i => i.StartPoints).SingleOrDefault(w => w.TaskName == taskName);
    }

    public TaskModel CreateTask(TaskModel newTask)
    {
        return _context.Tasks.Add(newTask).Entity;
    }

    public TaskModel UpdateTask(TaskModel task)
    {
        return _context.Update(task).Entity;
    }

    public TaskModel DeleteTask(TaskModel taskForDelete)
    {
        return _context.Tasks.Remove(taskForDelete).Entity;
    }

    public TaskStartPoint AddStartPoint(int taskId, string startPoint)
    {
        return _context.TaskStartPoints.Add(new TaskStartPoint { TaskId = taskId, StartPoint = startPoint }).Entity;
    }

    public TaskStartPoint? GetStartPoint(int taskId, string startPoint)
    {
        return _context.TaskStartPoints.SingleOrDefault(w => w.TaskId == taskId && w.StartPoint == startPoint);
    }

    public TaskStartPoint UpdateStartPoint(TaskStartPoint startPointForUpdate)
    {
        return _context.Update(startPointForUpdate).Entity;
    }

    public TaskStartPoint DeleteStartPoint(TaskStartPoint startPointForDelete)
    {
        return _context.TaskStartPoints.Remove(startPointForDelete).Entity;
    }

    #endregion

}
