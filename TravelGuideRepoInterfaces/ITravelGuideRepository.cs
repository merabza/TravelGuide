//Created by RepositoryInterfaceCreator at 7/24/2025 11:44:10 PM

using Microsoft.EntityFrameworkCore.Storage;
using TravelGuideDbModels;

namespace TravelGuideRepoInterfaces;

public interface ITravelGuideRepository
{
    bool NeedSaveChanges();
    int SaveChanges();
    int SaveChangesWithTransaction();
    IDbContextTransaction GetTransaction();

    List<TaskModel> GetTasksList();
    TaskModel? GetTaskByName(string taskName);
    TaskModel CreateTask(TaskModel newTask);
    TaskModel UpdateTask(TaskModel task);
    TaskModel DeleteTask(TaskModel taskForDelete);

    TaskStartPoint AddStartPoint(int taskId, string startPoint);
    TaskStartPoint? GetStartPoint(int taskId, string startPoint);
    TaskStartPoint UpdateStartPoint(TaskStartPoint startPointForUpdate);
    TaskStartPoint DeleteStartPoint(TaskStartPoint startPointForDelete);

    PlaceModel AddPlace(PlaceModel newPlace);
    Dictionary<string, int> GetPlaceIdsByUrl();
    List<PlaceModel> GetPlacesForAnalysis(bool includeAnalysed, bool includeDownloadErrors);
    bool HasAnalysedPlaces();
    bool HasDownloadErrorPlaces();
    List<PlaceModel> GetNearestPlaces(double latitude, double longitude, int count, TimeSpan minRoadTime);
    List<PlacePointItem> GetPlacePoints();

    UrlGraphNode AddUrlGraphNode(UrlGraphNode newUrlGraphNode);
    List<UrlGraphNode> GetAllUrlGraphNodes();

    List<MonthModel> GetMonths();
    MonthModel AddMonth(MonthModel newMonth);
    CategoryModel GetOrCreateCategory(string categoryName);
    TagModel GetOrCreateTag(string tagName);
    FromPointModel GetOrCreateFromPoint(string fromPointName);
    LocationModel GetOrCreateLocation(double latitude, double longitude);
    RegionModel GetOrCreateRegion(string regionName);
    MunicipalityModel GetOrCreateMunicipality(string municipalityName);

    List<MotorcycleModel> GetMotorcyclesList();
    MotorcycleModel? GetMotorcycleByKey(string key);
    MotorcycleModel CreateMotorcycle(MotorcycleModel newMotorcycle);
    MotorcycleModel UpdateMotorcycle(MotorcycleModel motorcycle);
    MotorcycleModel DeleteMotorcycle(MotorcycleModel motorcycleForDelete);

    VisitModel CreateVisit(VisitModel newVisit);
    List<VisitListItem> GetLastVisits(int count);

    RouteDistanceModel AddRouteDistance(RouteDistanceModel newRouteDistance);
    List<RouteDistanceModel> GetAllRouteDistances();
}
