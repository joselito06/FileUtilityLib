
namespace FileUtilityLib.Models.Events
{
    public class TaskScheduleEventArgs : EventArgs
    {
        public string TaskId { get; }
        public string TaskName { get; }
        public DateTime ScheduledTime { get; }
        public DateTime ActualStartTime { get; }

        public TaskScheduleEventArgs(string taskId, string taskName, DateTime scheduledTime, DateTime actualStartTime)
        {
            TaskId = taskId;
            TaskName = taskName;
            ScheduledTime = scheduledTime;
            ActualStartTime = actualStartTime;
        }
    }
}
