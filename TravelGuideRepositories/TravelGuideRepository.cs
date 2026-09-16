//Created by RepositoryClassCreator at 7/24/2025 11:44:10 PM

using DoTravelGuide;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using TravelGuideCore.Application.Abstractions;
using TravelGuideCore.Domain.CategoryModels;
using TravelGuideCore.Domain.FromPointModels;
using TravelGuideCore.Domain.LocationModels;
using TravelGuideCore.Domain.MonthModels;
using TravelGuideCore.Domain.MotorcycleModels;
using TravelGuideCore.Domain.MunicipalityModels;
using TravelGuideCore.Domain.PlaceModels;
using TravelGuideCore.Domain.PlacesByLocations;
using TravelGuideCore.Domain.RegionModels;
using TravelGuideCore.Domain.RouteDistanceModels;
using TravelGuideCore.Domain.TagModels;
using TravelGuideCore.Domain.TaskModels;
using TravelGuideCore.Domain.TaskStartPoints;
using TravelGuideCore.Domain.UrlGraphNodes;
using TravelGuideCore.Domain.UrlModels;
using TravelGuideCore.Domain.VisitImages;
using TravelGuideCore.Domain.VisitListItems;
using TravelGuideCore.Domain.VisitModels;
using TravelGuideRepoInterfaces;

namespace TravelGuideRepositories;

public sealed class TravelGuideRepository : ITravelGuideRepository
{
    private const int MaxChangesCount = 100000;
    private readonly ITravelGuideApplicationDbContext _context;
    private readonly ILogger<TravelGuideRepository> _logger;

    private int _changesCount;

    public TravelGuideRepository(ITravelGuideApplicationDbContext ctx, ILogger<TravelGuideRepository> logger)
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
        return _context.BeginTransaction();
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

    public List<PlaceModel> GetPlacesForAnalysis(bool includeAnalysed, bool includeDownloadErrors)
    {
        //ThenInclude აუცილებელია: ბმულების სინქრონიზაცია lookup-ობიექტების იგივეობით ადარებს და დაუტვირთავი ნავიგაცია გამონაკლისს ისვრის
        //რეგიონისა და მუნიციპალიტეტის ნავიგაციებიც იტვირთება, რომ ხელახალი ანალიზისას null-ის მინიჭებამ FK ნამდვილად გაასუფთაოს
        //NotAttraction გვერდები ხელახლა დამუშავებისასაც გამოტოვებულია — ისინი ღირსშესანიშნაობის გვერდები არ არის
        //Duplicate გვერდებიც სამუდამოდ გამოტოვებულია — მათ შიგთავსს კანონიკური მისამართის ჩანაწერი ფარავს
        //DownloadError გვერდები მხოლოდ მაშინ იტვირთება, როცა მომხმარებელმა მათი ხელახლა ცდა მოითხოვა
        //სტატუსი მისამართისაა (UrlModel.State) და ფილტრიც მისამართზეა; უმისამართო (ხელით შეყვანილი) ადგილები
        //ჩამოსატვირთი არ არის — მათ სტატუსი არ აქვთ და ქროულერი მათ არ ეხება; მისამართი (UrlNavigation)
        //ჩამოსატვირთი გვერდის მისამართისა და სტატუსის შესაცვლელად იტვირთება
        //AsSplitQuery: რამდენიმე კოლექციის ერთ SQL-ში ჩატვირთვა მწკრივებს კარტეზიულად ამრავლებს —
        //თითო კოლექცია ცალკე მოთხოვნით იტვირთება (დალაგება PlaceId-ით ცალსახაა, პორციები არ ირევა)
        return
        [
            .. _context.Places.Include(i => i.BestSeasons).Include(i => i.Categories)
                .ThenInclude(t => t.CategoryNavigation).Include(i => i.Tags).ThenInclude(t => t.TagNavigation)
                .Include(i => i.Distances).ThenInclude(t => t.FromPointNavigation).Include(i => i.Locations)
                .ThenInclude(t => t.LocationNavigation).Include(i => i.RegionNavigation)
                .Include(i => i.MunicipalityNavigation).Include(i => i.UrlNavigation).AsSplitQuery().Where(w =>
                    w.UrlNavigation != null && w.UrlNavigation.State != EState.NotAttraction &&
                    w.UrlNavigation.State != EState.Duplicate &&
                    (includeAnalysed || w.UrlNavigation.State != EState.Analysed) &&
                    (includeDownloadErrors || w.UrlNavigation.State != EState.DownloadError)).OrderBy(o => o.PlaceId)
        ];
    }

    //სტატუსი მისამართისაა — Urls ცხრილი მოწმდება: მისამართიანი ადგილი გაანალიზებულია, როცა მისი მისამართია გაანალიზებული
    public bool HasAnalysedPlaces()
    {
        return _context.Urls.Any(a => a.State == EState.Analysed);
    }

    public bool HasDownloadErrorPlaces()
    {
        return _context.Urls.Any(a => a.State == EState.DownloadError);
    }

    public int GetPlacesCount()
    {
        return _context.Places.Count();
    }

    public List<PlaceModel> GetPlacesPortion(string? filter, int skip, int take)
    {
        //ადგილების რედაქტორის სიის პორცია: ცხრილი ათასობით ჩანაწერს შეიცავს, ამიტომ სია ფილტრით
        //(დასახელების ან მისამართის ნაწილი) და პორციებად იტვირთება. დალაგება ცალსახაა (Skip/Take-ისთვის):
        //დასახელებით, უსახელო ჩანაწერებისთვის მისამართით, თანაბრებს PlaceId წყვეტს.
        //მოუბმელი ასლები ბრუნდება: ველების რედაქტორები მათ პირდაპირ ცვლიან და შეყვანის შეწყვეტისას
        //ნახევრად შეცვლილი ჩანაწერი საზიარო კონტექსტში არ უნდა დარჩეს — შენახვისას ბმული ჩანაწერი
        //GetPlaceById-ით ცალკე მოიძებნება. მისამართი (UrlNavigation) ასლებს ერთვის — წარწერასა და სტატუსში ჩანს
        IQueryable<PlaceModel> placesQuery = _context.Places.AsNoTracking().Include(i => i.UrlNavigation);
        if (!string.IsNullOrWhiteSpace(filter))
        {
            placesQuery = placesQuery.Where(w =>
                w.Name != null && w.Name.Contains(filter) ||
                w.UrlNavigation != null && w.UrlNavigation.Url.Contains(filter));
        }

        return
        [
            .. placesQuery.OrderBy(o => o.Name ?? o.UrlNavigation!.Url).ThenBy(o => o.PlaceId).Skip(skip).Take(take)
        ];
    }

    public PlaceModel? GetPlaceById(int placeId)
    {
        //მისამართის ჩანაწერიც იტვირთება — ადგილის წაშლისას ის ადგილთან ერთად იშლება
        return _context.Places.Include(i => i.UrlNavigation).SingleOrDefault(w => w.PlaceId == placeId);
    }

    public PlaceModel UpdatePlace(PlaceModel place)
    {
        return _context.Update(place).Entity;
    }

    public PlaceModel DeletePlace(PlaceModel placeForDelete)
    {
        return _context.Places.Remove(placeForDelete).Entity;
    }

    public List<LocationModel> GetPlaceLinkedLocations()
    {
        //ლოკაციები მხოლოდ კოორდინატების წასაკითხად იტვირთება და ენთითები კონტექსტს არ ებმება.
        //მხოლოდ ადგილებთან მიბმული ლოკაციები ბრუნდება (PlacesByLocations) — თითო ერთხელ, რამდენი ადგილიც არ
        //უნდა ეზიარებოდეს; ბმულის გარეშე დარჩენილი ლოკაციები არ ბრუნდება
        return
        [
            .. _context.Locations.AsNoTracking()
                .Where(w => _context.PlacesByLocations.Any(a => a.LocationId == w.LocationId))
                .OrderBy(o => o.LocationId)
        ];
    }

    public List<PlaceByLocation> GetPlaceLocations(int placeId)
    {
        //ერთი ადგილის ლოკაციების ბმულები კოორდინატებით — ადგილების რედაქტორის Locations ქვერედაქტორისთვის.
        //მოუბმელი ასლები ბრუნდება (რედაქტორი კოორდინატების საკუთარ ასლებზე მუშაობს); შესაცვლელი ან
        //წასაშლელი ბმული GetPlaceLocation-ით ცალკე მოიძებნება
        return
        [
            .. _context.PlacesByLocations.AsNoTracking().Include(i => i.LocationNavigation)
                .Where(w => w.PlaceId == placeId).OrderBy(o => o.LocationId)
        ];
    }

    public PlaceByLocation? GetPlaceLocation(int placeId, int locationId)
    {
        return _context.PlacesByLocations.SingleOrDefault(w => w.PlaceId == placeId && w.LocationId == locationId);
    }

    public PlaceByLocation AddPlaceLocation(int placeId, LocationModel location)
    {
        //ლოკაცია შეიძლება ახალი, ჯერ შეუნახავი იყოს (GetOrCreateLocation) — ბმული ნავიგაციით იწერება და
        //LocationId შენახვისას ივსება
        return _context.PlacesByLocations.Add(new PlaceByLocation { PlaceId = placeId, LocationNavigation = location })
            .Entity;
    }

    public PlaceByLocation DeletePlaceLocation(PlaceByLocation placeLocationForDelete)
    {
        return _context.PlacesByLocations.Remove(placeLocationForDelete).Entity;
    }

    public List<PlaceByLocation> GetNearestPlaces(LocationModel startLocation, int skip, int take, TimeSpan minRoadTime,
        TimeSpan maxRoadTime, int maxVisitsCount, EOrderVisitsBy orderVisitsBy)
    {
        //საწყისი წერტილი Locations ცხრილის ჩანაწერია: კოორდინატები საჰაერო მანძილს სჭირდება, იდენტიფიკატორი —
        //RouteDistances-ში დათვლილი მარშრუტების მოსაძებნად
        int startLocationId = startLocation.LocationId;
        double latitude = startLocation.Latitude;
        double longitude = startLocation.Longitude;

        //გრძედის გრადუსი განედის გრადუსზე მოკლეა, ამიტომ გრძედის სხვაობა განედის კოსინუსით სწორდება
        double cosLatitude = Math.Cos(latitude * Math.PI / 180);

        //თითო ჩანაწერი ადგილი-ლოკაციის ბმულია — მრავალლოკაციიანი ადგილი სიაში იმდენჯერ ჩნდება, რამდენი
        //ლოკაციაც აქვს (ულოკაციო ადგილი ბმულების გარეშე თავისთავად გამოირიცხება). ადგილის ნავიგაციები
        //ქვემენიუს საინფორმაციო პუნქტებისთვის იტვირთება, Locations — პუნქტის სახელში ლოკაციის რიგითი
        //ნომრის დასათვლელად (მხოლოდ ბმულები, სხვა ლოკაციების კოორდინატები საჭირო არ არის).
        //მისამართის სტატუსი (UrlModel.State) განზრახ არ მოწმდება — ლოკაციიანი ყველა ადგილი მონაწილეობს.
        //AsSplitQuery: რამდენიმე კოლექციის ერთ SQL-ში ჩატვირთვა მწკრივებს კარტეზიულად ამრავლებს —
        //თითო კოლექცია ცალკე მოთხოვნით იტვირთება (Skip/Take-ისთვის საჭირო ცალსახა დალაგება ქვემოთ უკვე დგას)
        IQueryable<PlaceByLocation> placeLocationsQuery = _context.PlacesByLocations.Include(i => i.LocationNavigation)
            .Include(i => i.PlaceNavigation).ThenInclude(t => t.BestSeasons).ThenInclude(t => t.MonthNavigation)
            .Include(i => i.PlaceNavigation).ThenInclude(t => t.Tags).ThenInclude(t => t.TagNavigation)
            .Include(i => i.PlaceNavigation).ThenInclude(t => t.Distances).ThenInclude(t => t.FromPointNavigation)
            .Include(i => i.PlaceNavigation).ThenInclude(t => t.Locations).Include(i => i.PlaceNavigation)
            .ThenInclude(t => t.RegionNavigation).Include(i => i.PlaceNavigation)
            .ThenInclude(t => t.MunicipalityNavigation).Include(i => i.PlaceNavigation)
            .ThenInclude(t => t.UrlNavigation).AsSplitQuery();

        //მინიმალური გზის დროის მოთხოვნისას რჩება მხოლოდ ის ლოკაციები, რომლებამდეც დათვლილი გზის დრო
        //ზღვარს აღწევს. მარშრუტი საწყისი და საბოლოო ლოკაციების იდენტიფიკატორებით იძებნება — RouteDistances
        //ორივე ბოლოს Locations ცხრილის ჩანაწერით ინახავს. დაუთვლელ ლოკაციას შესაბამისი ჩანაწერი არ აქვს და
        //განზრახ გამოირიცხება, სანამ საწყისი წერტილის რედაქტორის Calculate Distances არ დაითვლის
        if (minRoadTime > TimeSpan.Zero)
        {
            placeLocationsQuery = placeLocationsQuery.Where(w => _context.RouteDistances.Any(rd =>
                rd.StartLocationId == startLocationId && rd.EndLocationId == w.LocationId &&
                rd.RoadTime >= minRoadTime));
        }

        if (maxRoadTime < TimeSpan.MaxValue)
        {
            placeLocationsQuery = placeLocationsQuery.Where(w => _context.RouteDistances.Any(rd =>
                rd.StartLocationId == startLocationId && rd.EndLocationId == w.LocationId &&
                rd.RoadTime <= maxRoadTime));
        }

        //რჩება მხოლოდ ის ლოკაციები, რომლებზეც დაფიქსირებული ვიზიტების რაოდენობა მოთხოვნილ მაქსიმუმს
        //არ აღემატება — ვიზიტი ლოკაციაზეა და ფილტრიც ლოკაციაზეა, ამიტომ მრავალლოკაციიანი ადგილის
        //მოუნახულებელი ლოკაციები სიაში რჩება მაშინაც, როცა მისი სხვა ლოკაცია უკვე ნანახია.
        //0-ის მოთხოვნისას მხოლოდ მოუნახულებელი ლოკაციები რჩება
        placeLocationsQuery = placeLocationsQuery.Where(w =>
            _context.Visits.Count(c => c.LocationId == w.LocationId) <= maxVisitsCount);

        //თითო ბმულს დალაგების ნიშნები ერთვის: საჰაერო მანძილის კვადრატი (ფესვის ამოღება რიგითობას არ
        //ცვლის) და საწყისი ლოკაციიდან ამ ლოკაციამდე დათვლილი მარშრუტი — დაუთვლელისთვის null. მარშრუტი
        //ლოკაციების იდენტიფიკატორებით იძებნება, როგორც ზემოთ გზის დროის ფილტრში
        var placeLocationsWithRoutes = placeLocationsQuery.Select(s => new
        {
            PlaceByLocation = s,
            AirDistanceSquare =
                (s.LocationNavigation.Latitude - latitude) * (s.LocationNavigation.Latitude - latitude) +
                (s.LocationNavigation.Longitude - longitude) * cosLatitude *
                (s.LocationNavigation.Longitude - longitude) * cosLatitude,
            Route = _context.RouteDistances.FirstOrDefault(rd =>
                rd.StartLocationId == startLocationId && rd.EndLocationId == s.LocationId)
        });

        //პარამეტრებში არჩეული ნიშნით დალაგება. გზის დროით ან გზის მანძილით დალაგებისას დაუთვლელი
        //მარშრუტის ლოკაციები სიის ბოლოში გადადის (სანამ Calculate Distances არ დაითვლის), თანაბარ
        //მნიშვნელობებს კი საჰაერო მანძილი წყვეტს; დანარჩენი (AirDistance) საჰაერო მანძილით ლაგდება
        var orderedQuery = orderVisitsBy switch
        {
            EOrderVisitsBy.RoadTime => placeLocationsWithRoutes.OrderBy(o => o.Route == null)
                .ThenBy(o => o.Route!.RoadTime).ThenBy(o => o.AirDistanceSquare),
            EOrderVisitsBy.RoadDistance => placeLocationsWithRoutes.OrderBy(o => o.Route == null)
                .ThenBy(o => o.Route!.RoadDistance).ThenBy(o => o.AirDistanceSquare),
            _ => placeLocationsWithRoutes.OrderBy(o => o.AirDistanceSquare)
        };

        //სია პორციებად იტვირთება (Skip/Take → OFFSET/FETCH), ამიტომ დალაგება ცალსახა უნდა იყოს —
        //თანაბარი მნიშვნელობებისას PlaceId+LocationId წყვეტს, თორემ ჩანაწერი ორ პორციაში მოხვდებოდა
        //ან საერთოდ გამორჩებოდა
        return
        [
            .. orderedQuery.ThenBy(o => o.PlaceByLocation.PlaceId).ThenBy(o => o.PlaceByLocation.LocationId).Skip(skip)
                .Take(take).Select(s => s.PlaceByLocation)
        ];
    }

    #endregion

    #region Url cruder

    public Dictionary<string, int> GetUrlIdsByUrlHashCode(int urlHashCode)
    {
        //ინდექსირებული ხეშ-კოდით ამოკრებილი (ჩვეულებრივ 0 ან 1) ჩანაწერი — Url-ის ზუსტ შედარებას გამომძახებელი
        //აკეთებს; მხოლოდ ორი სვეტი იტვირთება და ენთითები კონტექსტს არ ებმება
        return _context.Urls.Where(w => w.UrlHashCode == urlHashCode).Select(s => new { s.Url, s.UrlId })
            .ToDictionary(k => k.Url, v => v.UrlId, StringComparer.Ordinal);
    }

    public UrlModel DeleteUrl(UrlModel urlForDelete)
    {
        return _context.Urls.Remove(urlForDelete).Entity;
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

    public void DeleteUrlGraphNodesByUrlId(int urlId)
    {
        //მისამართის წაშლისას მისი გრაფის წიბოები წინასწარ უნდა წაიშალოს — ორივე FK Restrict-ია და ბაზა
        //მისამართს კავშირებთან ერთად არ წაშლიდა. ჩანაწერები კონტექსტში იშლება და მისამართთან ერთად, ერთი
        //SaveChanges-ით ინახება
        _context.UrlGraphNodes.RemoveRange(_context.UrlGraphNodes.Where(w =>
            w.FromUrlId == urlId || w.GotUrlId == urlId));
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

    public LocationModel GetOrCreateLocation(double latitude, double longitude)
    {
        //ზუსტი ტოლობა განზრახაა: მნიშვნელობები ერთი და იმავე ტექსტიდანაა გაპარსული და ბაზაშიც
        //(Latitude, Longitude) უნიკალური ინდექსი ზუსტ ტოლობას ამოწმებს — მიახლოებითი შედარება
        //ინდექსის სემანტიკას ასცდებოდა და კონფლიქტს გამოიწვევდა
#pragma warning disable S1244
        LocationModel? location =
            _context.Locations.Local.FirstOrDefault(f => f.Latitude == latitude && f.Longitude == longitude) ??
            _context.Locations.FirstOrDefault(f => f.Latitude == latitude && f.Longitude == longitude);
#pragma warning restore S1244
        return location ?? _context.Locations.Add(new LocationModel { Latitude = latitude, Longitude = longitude })
            .Entity;
    }

    public RegionModel GetOrCreateRegion(string regionName)
    {
        RegionModel? region = _context.Regions.Local.FirstOrDefault(f => f.Name == regionName) ??
                              _context.Regions.FirstOrDefault(f => f.Name == regionName);
        return region ?? _context.Regions.Add(new RegionModel { Name = regionName }).Entity;
    }

    public MunicipalityModel GetOrCreateMunicipality(string municipalityName)
    {
        MunicipalityModel? municipality =
            _context.Municipalities.Local.FirstOrDefault(f => f.Name == municipalityName) ??
            _context.Municipalities.FirstOrDefault(f => f.Name == municipalityName);
        return municipality ?? _context.Municipalities.Add(new MunicipalityModel { Name = municipalityName }).Entity;
    }

    public List<RegionModel> GetRegionsList()
    {
        //ცნობარი მხოლოდ ასარჩევად იტვირთება და ენთითები კონტექსტს არ ებმება
        return [.. _context.Regions.AsNoTracking().OrderBy(o => o.Name)];
    }

    public List<MunicipalityModel> GetMunicipalitiesList()
    {
        return [.. _context.Municipalities.AsNoTracking().OrderBy(o => o.Name)];
    }

    public RegionModel? GetRegionByName(string regionName)
    {
        //სახელი ბაზის შედარებით (collation) მოწმდება, რომ რედაქტორის უნიკალურობის შემოწმება ინდექსს დაემთხვეს
        return _context.Regions.AsNoTracking().FirstOrDefault(f => f.Name == regionName);
    }

    public RegionModel? GetRegionById(int regionId)
    {
        return _context.Regions.SingleOrDefault(w => w.RegionId == regionId);
    }

    public RegionModel UpdateRegion(RegionModel region)
    {
        return _context.Update(region).Entity;
    }

    public RegionModel DeleteRegion(RegionModel regionForDelete)
    {
        return _context.Regions.Remove(regionForDelete).Entity;
    }

    //რამდენი ადგილი ეყრდნობა რეგიონს — რედაქტორი გამოყენებულ ჩანაწერს არ შლის
    public int GetPlacesCountByRegionId(int regionId)
    {
        return _context.Places.Count(c => c.RegionId == regionId);
    }

    public MunicipalityModel? GetMunicipalityByName(string municipalityName)
    {
        return _context.Municipalities.AsNoTracking().FirstOrDefault(f => f.Name == municipalityName);
    }

    public MunicipalityModel? GetMunicipalityById(int municipalityId)
    {
        return _context.Municipalities.SingleOrDefault(w => w.MunicipalityId == municipalityId);
    }

    public MunicipalityModel UpdateMunicipality(MunicipalityModel municipality)
    {
        return _context.Update(municipality).Entity;
    }

    public MunicipalityModel DeleteMunicipality(MunicipalityModel municipalityForDelete)
    {
        return _context.Municipalities.Remove(municipalityForDelete).Entity;
    }

    public int GetPlacesCountByMunicipalityId(int municipalityId)
    {
        return _context.Places.Count(c => c.MunicipalityId == municipalityId);
    }

    public List<CategoryModel> GetCategoriesList()
    {
        //ცნობარები რედაქტორისთვის მოუბმელად იტვირთება, როგორც GetRegionsList
        return [.. _context.Categories.AsNoTracking().OrderBy(o => o.Name)];
    }

    public CategoryModel? GetCategoryByName(string categoryName)
    {
        return _context.Categories.AsNoTracking().FirstOrDefault(f => f.Name == categoryName);
    }

    public CategoryModel? GetCategoryById(int categoryId)
    {
        return _context.Categories.SingleOrDefault(w => w.CategoryId == categoryId);
    }

    public CategoryModel UpdateCategory(CategoryModel category)
    {
        return _context.Update(category).Entity;
    }

    public CategoryModel DeleteCategory(CategoryModel categoryForDelete)
    {
        return _context.Categories.Remove(categoryForDelete).Entity;
    }

    //რამდენი ადგილია კატეგორიაზე მიბმული — რედაქტორი გამოყენებულ ჩანაწერს არ შლის (ბმულები კასკადით წაიშლებოდა)
    public int GetPlacesCountByCategoryId(int categoryId)
    {
        return _context.PlacesByCategories.Count(c => c.CategoryId == categoryId);
    }

    public List<TagModel> GetTagsList()
    {
        return [.. _context.Tags.AsNoTracking().OrderBy(o => o.Name)];
    }

    public TagModel? GetTagByName(string tagName)
    {
        return _context.Tags.AsNoTracking().FirstOrDefault(f => f.Name == tagName);
    }

    public TagModel? GetTagById(int tagId)
    {
        return _context.Tags.SingleOrDefault(w => w.TagId == tagId);
    }

    public TagModel UpdateTag(TagModel tag)
    {
        return _context.Update(tag).Entity;
    }

    public TagModel DeleteTag(TagModel tagForDelete)
    {
        return _context.Tags.Remove(tagForDelete).Entity;
    }

    public int GetPlacesCountByTagId(int tagId)
    {
        return _context.PlacesByTags.Count(c => c.TagId == tagId);
    }

    public List<FromPointModel> GetFromPointsList()
    {
        //მდებარეობა ცნობარის რედაქტორისთვის იტვირთება
        return [.. _context.FromPoints.AsNoTracking().Include(i => i.LocationNavigation).OrderBy(o => o.Name)];
    }

    public FromPointModel? GetFromPointByName(string fromPointName)
    {
        return _context.FromPoints.AsNoTracking().FirstOrDefault(f => f.Name == fromPointName);
    }

    public FromPointModel? GetFromPointById(int fromPointId)
    {
        return _context.FromPoints.SingleOrDefault(w => w.FromPointId == fromPointId);
    }

    public FromPointModel UpdateFromPoint(FromPointModel fromPoint)
    {
        return _context.Update(fromPoint).Entity;
    }

    public FromPointModel DeleteFromPoint(FromPointModel fromPointForDelete)
    {
        return _context.FromPoints.Remove(fromPointForDelete).Entity;
    }

    //თითო ადგილს საწყისი წერტილიდან ერთი მანძილი აქვს (უნიკალური წყვილი), ამიტომ მანძილების რაოდენობა ადგილების რაოდენობაა
    public int GetPlacesCountByFromPointId(int fromPointId)
    {
        return _context.DistanceByPlaces.Count(c => c.FromPointId == fromPointId);
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

    #region Visit cruder

    public VisitModel CreateVisit(VisitModel newVisit)
    {
        return _context.Visits.Add(newVisit).Entity;
    }

    public List<VisitListItem> GetLastVisits(int count)
    {
        //ვიზიტს ნავიგაციები არ აქვს, ამიტომ ლოკაციის კოორდინატები და მოტოციკლის სახელი შეერთებით მოიპოვება.
        //ადგილის სახელი ლოკაციაზე მიბმული ადგილიდან მოდის (PlacesByLocations): საზიარო ლოკაციისას პირველი ისეთი
        //ადგილი აიღება, რომლის მისამართიც დუბლიკატად არ არის მონიშნული (უმისამართოს სტატუსი არ აქვს), ხოლო
        //არცერთ ადგილს რომ არ ებმებოდეს — null.
        //დალაგება და შეზღუდვა შეერთებების შემდეგ კეთდება, რომ ერთი მოწესრიგებული მოთხოვნა შესრულდეს
        return
        [
            .. _context.Visits
                .Join(_context.Locations, v => v.LocationId, l => l.LocationId,
                    (v, l) => new
                    {
                        v.VisitId, v.VisitDate, v.LocationId, l.Latitude, l.Longitude, v.MotorcycleId
                    })
                .Join(_context.Motorcycles, v => v.MotorcycleId, m => m.MotorcycleId,
                    (v, m) => new
                    {
                        v.VisitId, v.VisitDate, v.LocationId, v.Latitude, v.Longitude, m.MotorcycleKey
                    })
                .OrderByDescending(o => o.VisitDate).ThenByDescending(o => o.VisitId).Take(count).Select(s =>
                    new VisitListItem
                    {
                        VisitDate = s.VisitDate,
                        PlaceName = _context.PlacesByLocations
                            .Where(w => w.LocationId == s.LocationId &&
                                        (w.PlaceNavigation.UrlNavigation == null ||
                                         w.PlaceNavigation.UrlNavigation.State != EState.Duplicate))
                            .OrderBy(o => o.PlaceId)
                            .Select(p => p.PlaceNavigation.Name ?? p.PlaceNavigation.UrlNavigation!.Url)
                            .FirstOrDefault(),
                        Latitude = s.Latitude,
                        Longitude = s.Longitude,
                        MotorcycleKey = s.MotorcycleKey
                    })
        ];
    }

    public List<VisitModel> GetVisitsByLocationId(int locationId)
    {
        //ერთი ლოკაციის ვიზიტები თარიღის კლებადობით — ბოლო ვიზიტი სიის თავშია.
        //მოუბმელი ასლები ბრუნდება: ველების რედაქტორები მათ პირდაპირ ცვლიან და შეყვანის შეწყვეტისას
        //ნახევრად შეცვლილი ჩანაწერი საზიარო კონტექსტში არ უნდა დარჩეს — შენახვისას ბმული ჩანაწერი
        //GetVisitById-ით ცალკე მოიძებნება.
        //სურათები ვიზიტთან ერთად იტვირთება — მათი რაოდენობა ველის რედაქტორის სტატუსში ჩანს
        return
        [
            .. _context.Visits.AsNoTracking().Include(i => i.Images).Where(w => w.LocationId == locationId)
                .OrderByDescending(o => o.VisitDate).ThenByDescending(o => o.VisitId)
        ];
    }

    public Dictionary<int, int> GetVisitCountsByLocationIds(List<int> locationIds)
    {
        //ერთი მოთხოვნით ითვლება, თითო ლოკაციაზე რამდენი ვიზიტია დაფიქსირებული — უვიზიტო ლოკაცია
        //ლექსიკონში საერთოდ არ ჩნდება და გამომძახებელმა ნულად უნდა აღიქვას
        return _context.Visits.Where(w => locationIds.Contains(w.LocationId)).GroupBy(g => g.LocationId)
            .Select(s => new { LocationId = s.Key, Count = s.Count() })
            .ToDictionary(k => k.LocationId, v => v.Count);
    }

    public VisitModel? GetVisitById(int visitId)
    {
        return _context.Visits.SingleOrDefault(w => w.VisitId == visitId);
    }

    public VisitModel UpdateVisit(VisitModel visit)
    {
        return _context.Update(visit).Entity;
    }

    public VisitModel DeleteVisit(VisitModel visitForDelete)
    {
        return _context.Visits.Remove(visitForDelete).Entity;
    }

    public List<VisitImage> GetVisitImages(int visitId)
    {
        return [.. _context.VisitImages.Where(w => w.VisitId == visitId).OrderBy(o => o.FileName)];
    }

    public VisitImage? GetVisitImage(int visitId, string fileName)
    {
        return _context.VisitImages.SingleOrDefault(w => w.VisitId == visitId && w.FileName == fileName);
    }

    public VisitImage AddVisitImage(int visitId, string fileName)
    {
        return _context.VisitImages.Add(new VisitImage { VisitId = visitId, FileName = fileName }).Entity;
    }

    public VisitImage DeleteVisitImage(VisitImage visitImageForDelete)
    {
        return _context.VisitImages.Remove(visitImageForDelete).Entity;
    }

    #endregion

    #region RouteDistance cruder

    public RouteDistanceModel AddRouteDistance(RouteDistanceModel newRouteDistance)
    {
        return _context.RouteDistances.Add(newRouteDistance).Entity;
    }

    //წყვილი საწყისი და საბოლოო ლოკაციების იდენტიფიკატორებით იძებნება — უნიკალური ინდექსიც ამ წყვილზეა
    public RouteDistanceModel? GetRouteDistance(int startLocationId, int endLocationId)
    {
        return _context.RouteDistances.FirstOrDefault(f =>
            f.StartLocationId == startLocationId && f.EndLocationId == endLocationId);
    }

    public List<RouteDistanceModel> GetRouteDistancesByStartLocationId(int startLocationId)
    {
        //ერთი საწყისი ლოკაციიდან დათვლილი ყველა მარშრუტი. ჩანაწერები კონტექსტს ებმება, რადგან ხელახალი
        //გამოთვლისას მნიშვნელობები ადგილზე სწორდება
        return [.. _context.RouteDistances.Where(w => w.StartLocationId == startLocationId)];
    }

    #endregion
}
