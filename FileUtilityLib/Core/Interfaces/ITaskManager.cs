using FileUtilityLib.Models;

namespace FileUtilityLib.Core.Interfaces
{
    public interface ITaskManager
    {
        string AddTask(FileCopyTask task);
        bool UpdateTask(FileCopyTask task);
        bool RemoveTask(string taskId);
        FileCopyTask? GetTask(string taskId);
        List<FileCopyTask> GetAllTasks();
        List<FileCopyTask> GetEnabledTasks();
        Task SaveTasksAsync();
        Task LoadTasksAsync();
    }
}
