using FileUtilityLib.Models.Events;
using FileUtilityLib.Models;

namespace FileUtilityLib.Core.Interfaces
{
    public interface IFileUtilityService : IDisposable
    {
        // Services
        IFileCopyService FileCopyService { get; }
        ITaskManager TaskManager { get; }
        ISchedulerService SchedulerService { get; }

        // Events
        event EventHandler<CopyOperationEventArgs>? OperationStarted;
        event EventHandler<CopyOperationEventArgs>? OperationCompleted;
        event EventHandler<FileOperationEventArgs>? FileProcessing;
        event EventHandler<FileOperationEventArgs>? FileProcessed;
        event EventHandler<TaskScheduleEventArgs>? TaskScheduled;
        event EventHandler<TaskScheduleEventArgs>? TaskExecuting;

        // Main operations
        Task<string> CreateTaskAsync(FileCopyTask task, ScheduleConfiguration? schedule = null);
        Task<bool> UpdateTaskAsync(FileCopyTask task, ScheduleConfiguration? schedule = null);
        Task<bool> DeleteTaskAsync(string taskId);
        Task<CopyOperationResult> ExecuteTaskNowAsync(string taskId, CancellationToken cancellationToken = default);

        // Scheduler operations
        Task StartSchedulerAsync();
        Task StopSchedulerAsync();
        bool IsSchedulerRunning { get; }

        // Query operations
        List<FileCopyTask> GetAllTasks();
        List<ScheduleConfiguration> GetAllSchedules();
        Task<List<DateTime>> GetNextExecutionTimesAsync(string taskId, int count = 5);

        // Configuration
        Task SaveConfigurationAsync();
        Task LoadConfigurationAsync();
    }
}
