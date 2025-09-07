
namespace FileUtilityLib.Models
{
    public class ScheduleConfiguration
    {
        public string TaskId { get; set; } = string.Empty;
        public ScheduleType Type { get; set; }
        public List<TimeSpan> ExecutionTimes { get; set; } = new(); // 8:00 AM, 9:00 AM, etc.
        public List<DayOfWeek> DaysOfWeek { get; set; } = new(); // Monday to Friday
        public int IntervalMinutes { get; set; } = 60;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsEnabled { get; set; } = true;
    }
}
