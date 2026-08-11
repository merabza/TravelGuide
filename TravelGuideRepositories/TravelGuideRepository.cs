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
        return [.. _context.Tasks.Include(i => i.StartPoints)];
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

    #region Place cruder

    public PlaceModel AddPlace(PlaceModel newPlace)
    {
        return _context.Places.Add(newPlace).Entity;
    }

    public Dictionary<string, int> GetPlaceIdsByUrl()
    {
        //მხოლოდ ორი სვეტი იტვირთება და ენთითები კონტექსტს არ ებმება
        return _context.Places.Select(s => new { s.Url, s.PlaceId })
            .ToDictionary(k => k.Url, v => v.PlaceId, StringComparer.Ordinal);
    }

    public List<PlaceModel> GetPlacesForAnalysis(bool includeAnalysed, bool includeDownloadErrors)
    {
        //ThenInclude აუცილებელია: ბმულების სინქრონიზაცია lookup-ობიექტების იგივეობით ადარებს და დაუტვირთავი ნავიგაცია გამონაკლისს ისვრის
        //რეგიონისა და მუნიციპალიტეტის ნავიგაციებიც იტვირთება, რომ ხელახალი ანალიზისას null-ის მინიჭებამ FK ნამდვილად გაასუფთაოს
        //NotAttraction გვერდები ხელახლა დამუშავებისასაც გამოტოვებულია — ისინი ღირსშესანიშნაობის გვერდები არ არის
        //DownloadError გვერდები მხოლოდ მაშინ იტვირთება, როცა მომხმარებელმა მათი ხელახლა ცდა მოითხოვა
        return
        [
            .. _context.Places.Include(i => i.BestSeasons)
                .Include(i => i.Categories).ThenInclude(t => t.CategoryNavigation)
                .Include(i => i.Tags).ThenInclude(t => t.TagNavigation)
                .Include(i => i.Distances).ThenInclude(t => t.FromPointNavigation)
                .Include(i => i.RegionNavigation)
                .Include(i => i.MunicipalityNavigation)
                .Where(w => w.State != EState.NotAttraction && (includeAnalysed || w.State != EState.Analysed) &&
                            (includeDownloadErrors || w.State != EState.DownloadError))
                .OrderBy(o => o.PlaceId)
        ];
    }

    public bool HasAnalysedPlaces()
    {
        return _context.Places.Any(a => a.State == EState.Analysed);
    }

    public bool HasDownloadErrorPlaces()
    {
        return _context.Places.Any(a => a.State == EState.DownloadError);
    }

    #endregion

    #region UrlGraphNode cruder

    public UrlGraphNode AddUrlGraphNode(UrlGraphNode newUrlGraphNode)
    {
        return _context.UrlGraphNodes.Add(newUrlGraphNode).Entity;
    }

    public List<UrlGraphNode> GetAllUrlGraphNodes()
    {
        //მხოლოდ გამეორებული კავშირების გასაფილტრად იკითხება — კონტექსტს მიბმა არ სჭირდება
        return [.. _context.UrlGraphNodes.AsNoTracking()];
    }

    #endregion

    #region Lookup cruder

    public List<MonthModel> GetMonths()
    {
        return [.. _context.Months.OrderBy(o => o.MonthId)];
    }

    public MonthModel AddMonth(MonthModel newMonth)
    {
        return _context.Months.Add(newMonth).Entity;
    }

    public CategoryModel GetOrCreateCategory(string categoryName)
    {
        //ჯერ Local მოწმდება, რომ ერთი გაშვების ფარგლებში ჯერ შეუნახავი სახელი მეორედ არ დაემატოს
        CategoryModel? category = _context.Categories.Local.FirstOrDefault(f => f.Name == categoryName) ??
                                  _context.Categories.FirstOrDefault(f => f.Name == categoryName);
        return category ?? _context.Categories.Add(new CategoryModel { Name = categoryName }).Entity;
    }

    public TagModel GetOrCreateTag(string tagName)
    {
        TagModel? tag = _context.Tags.Local.FirstOrDefault(f => f.Name == tagName) ??
                        _context.Tags.FirstOrDefault(f => f.Name == tagName);
        return tag ?? _context.Tags.Add(new TagModel { Name = tagName }).Entity;
    }

    public FromPointModel GetOrCreateFromPoint(string fromPointName)
    {
        FromPointModel? fromPoint = _context.FromPoints.Local.FirstOrDefault(f => f.Name == fromPointName) ??
                                    _context.FromPoints.FirstOrDefault(f => f.Name == fromPointName);
        return fromPoint ?? _context.FromPoints.Add(new FromPointModel { Name = fromPointName }).Entity;
    }

    public RegionModel GetOrCreateRegion(string regionName)
    {
        RegionModel? region = _context.Regions.Local.FirstOrDefault(f => f.Name == regionName) ??
                              _context.Regions.FirstOrDefault(f => f.Name == regionName);
        return region ?? _context.Regions.Add(new RegionModel { Name = regionName }).Entity;
    }

    public MunicipalityModel GetOrCreateMunicipality(string municipalityName)
    {
        MunicipalityModel? municipality = _context.Municipalities.Local.FirstOrDefault(f =>
                                              f.Name == municipalityName) ??
                                          _context.Municipalities.FirstOrDefault(f => f.Name == municipalityName);
        return municipality ?? _context.Municipalities.Add(new MunicipalityModel { Name = municipalityName }).Entity;
    }

    #endregion

    #region Motorcycle cruder

    public List<MotorcycleModel> GetMotorcyclesList()
    {
        return [.. _context.Motorcycles];
    }

    public MotorcycleModel? GetMotorcycleByKey(string key)
    {
        return _context.Motorcycles.SingleOrDefault(w => w.MotorcycleKey == key);
    }

    public MotorcycleModel CreateMotorcycle(MotorcycleModel newMotorcycle)
    {
        return _context.Motorcycles.Add(newMotorcycle).Entity;
    }

    public MotorcycleModel UpdateMotorcycle(MotorcycleModel motorcycle)
    {
        return _context.Update(motorcycle).Entity;
    }

    public MotorcycleModel DeleteMotorcycle(MotorcycleModel motorcycleForDelete)
    {
        return _context.Motorcycles.Remove(motorcycleForDelete).Entity;
    }

    #endregion
}
