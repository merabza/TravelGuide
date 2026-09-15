//Created by RepositoryInterfaceCreator at 7/24/2025 11:44:10 PM

using DoTravelGuide;
using Microsoft.EntityFrameworkCore.Storage;
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
using TravelGuideCore.Domain.VisitImages;
using TravelGuideCore.Domain.VisitListItems;
using TravelGuideCore.Domain.VisitModels;

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
    int GetPlacesCount();
    List<PlaceModel> GetPlacesPortion(string? filter, int skip, int take);
    PlaceModel? GetPlaceById(int placeId);
    PlaceModel UpdatePlace(PlaceModel place);
    PlaceModel DeletePlace(PlaceModel placeForDelete);

    List<PlaceByLocation> GetNearestPlaces(LocationModel startLocation, int skip, int take, TimeSpan minRoadTime,
        TimeSpan maxRoadTime, int maxVisitsCount, EOrderVisitsBy orderVisitsBy);

    List<LocationModel> GetPlaceLinkedLocations();

    List<PlaceByLocation> GetPlaceLocations(int placeId);
    PlaceByLocation? GetPlaceLocation(int placeId, int locationId);
    PlaceByLocation AddPlaceLocation(int placeId, LocationModel location);
    PlaceByLocation DeletePlaceLocation(PlaceByLocation placeLocationForDelete);

    UrlGraphNode AddUrlGraphNode(UrlGraphNode newUrlGraphNode);
    List<UrlGraphNode> GetAllUrlGraphNodes();
    void DeleteUrlGraphNodesByPlaceId(int placeId);

    List<MonthModel> GetMonths();
    MonthModel AddMonth(MonthModel newMonth);
    CategoryModel GetOrCreateCategory(string categoryName);
    TagModel GetOrCreateTag(string tagName);
    FromPointModel GetOrCreateFromPoint(string fromPointName);
    LocationModel GetOrCreateLocation(double latitude, double longitude);
    RegionModel GetOrCreateRegion(string regionName);
    MunicipalityModel GetOrCreateMunicipality(string municipalityName);
    List<RegionModel> GetRegionsList();
    List<MunicipalityModel> GetMunicipalitiesList();
    RegionModel? GetRegionByName(string regionName);
    RegionModel? GetRegionById(int regionId);
    RegionModel UpdateRegion(RegionModel region);
    RegionModel DeleteRegion(RegionModel regionForDelete);
    int GetPlacesCountByRegionId(int regionId);
    MunicipalityModel? GetMunicipalityByName(string municipalityName);
    MunicipalityModel? GetMunicipalityById(int municipalityId);
    MunicipalityModel UpdateMunicipality(MunicipalityModel municipality);
    MunicipalityModel DeleteMunicipality(MunicipalityModel municipalityForDelete);
    int GetPlacesCountByMunicipalityId(int municipalityId);
    List<CategoryModel> GetCategoriesList();
    CategoryModel? GetCategoryByName(string categoryName);
    CategoryModel? GetCategoryById(int categoryId);
    CategoryModel UpdateCategory(CategoryModel category);
    CategoryModel DeleteCategory(CategoryModel categoryForDelete);
    int GetPlacesCountByCategoryId(int categoryId);
    List<TagModel> GetTagsList();
    TagModel? GetTagByName(string tagName);
    TagModel? GetTagById(int tagId);
    TagModel UpdateTag(TagModel tag);
    TagModel DeleteTag(TagModel tagForDelete);
    int GetPlacesCountByTagId(int tagId);
    List<FromPointModel> GetFromPointsList();
    FromPointModel? GetFromPointByName(string fromPointName);
    FromPointModel? GetFromPointById(int fromPointId);
    FromPointModel UpdateFromPoint(FromPointModel fromPoint);
    FromPointModel DeleteFromPoint(FromPointModel fromPointForDelete);
    int GetPlacesCountByFromPointId(int fromPointId);

    List<MotorcycleModel> GetMotorcyclesList();
    MotorcycleModel? GetMotorcycleByKey(string key);
    MotorcycleModel CreateMotorcycle(MotorcycleModel newMotorcycle);
    MotorcycleModel UpdateMotorcycle(MotorcycleModel motorcycle);
    MotorcycleModel DeleteMotorcycle(MotorcycleModel motorcycleForDelete);

    VisitModel CreateVisit(VisitModel newVisit);
    List<VisitListItem> GetLastVisits(int count);
    List<VisitModel> GetVisitsByLocationId(int locationId);
    Dictionary<int, int> GetVisitCountsByLocationIds(List<int> locationIds);
    VisitModel? GetVisitById(int visitId);
    VisitModel UpdateVisit(VisitModel visit);
    VisitModel DeleteVisit(VisitModel visitForDelete);

    List<VisitImage> GetVisitImages(int visitId);
    VisitImage? GetVisitImage(int visitId, string fileName);
    VisitImage AddVisitImage(int visitId, string fileName);
    VisitImage DeleteVisitImage(VisitImage visitImageForDelete);

    RouteDistanceModel AddRouteDistance(RouteDistanceModel newRouteDistance);
    RouteDistanceModel? GetRouteDistance(int startLocationId, int endLocationId);
    List<RouteDistanceModel> GetRouteDistancesByStartLocationId(int startLocationId);
}
