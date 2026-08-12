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
    Dictionary<string, int> GetPlaceIdsByUrlHashCode(int urlHashCode);
    List<PlaceModel> GetPlacesForAnalysis(bool includeAnalysed, bool includeDownloadErrors);
    bool HasAnalysedPlaces();
    bool HasDownloadErrorPlaces();
    List<PlaceByLocation> GetNearestPlaces(double latitude, double longitude, int skip, int take,
        TimeSpan minRoadTime, int maxVisitsCount);
    List<LocationModel> GetAllLocations();

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
    List<VisitModel> GetVisitsByPlaceId(int placeId);
    Dictionary<int, int> GetVisitCountsByPlaceIds(List<int> placeIds);
    VisitModel? GetVisitById(int visitId);
    VisitModel UpdateVisit(VisitModel visit);
    VisitModel DeleteVisit(VisitModel visitForDelete);

    RouteDistanceModel AddRouteDistance(RouteDistanceModel newRouteDistance);

    RouteDistanceModel? GetRouteDistance(double startLatitude, double startLongitude, double endLatitude,
        double endLongitude);

    List<RouteDistanceModel> GetAllRouteDistances();
}
