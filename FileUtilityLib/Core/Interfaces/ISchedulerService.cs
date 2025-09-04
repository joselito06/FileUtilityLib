using FileUtilityLib.Models.Events;
using FileUtilityLib.Models;

namespace FileUtilityLib.Core.Interfaces
{
    public interface ISchedulerService
    {
        event EventHandler<TaskScheduleEventArgs>? TaskScheduled;
        event EventHandler<TaskScheduleEventArgs>? TaskExecuting;

        Task StartAsync();
        Task StopAsync();
        bool IsRunning { get; }

        Task ScheduleTaskAsync(string taskId, ScheduleConfiguration schedule);
        Task UnscheduleTaskAsync(string taskId);
        Task<bool> UpdateScheduleAsync(string taskId, ScheduleConfiguration schedule);

        List<ScheduleConfiguration> GetAllSchedules();
        ScheduleConfiguration? GetSchedule(string taskId);

        Task<DateTime?> GetNextExecutionTime(string taskId);
        List<DateTime> GetNextExecutionTimes(string taskId, int count = 5);
    }
}
