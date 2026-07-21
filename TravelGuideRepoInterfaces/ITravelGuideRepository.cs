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

}
