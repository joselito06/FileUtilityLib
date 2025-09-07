using FileUtilityLib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileUtilityLib.Extensions
{
    public static class ScheduleConfigurationExtensions
    {
        public static ScheduleConfiguration AddExecutionTime(this ScheduleConfiguration schedule, TimeSpan time)
        {
            schedule.ExecutionTimes.Add(time);
            return schedule;
        }

        public static ScheduleConfiguration AddExecutionTime(this ScheduleConfiguration schedule, int hour, int minute = 0)
        {
            schedule.ExecutionTimes.Add(new TimeSpan(hour, minute, 0));
            return schedule;
        }

        public static ScheduleConfiguration AddExecutionTimes(this ScheduleConfiguration schedule, params TimeSpan[] times)
        {
            schedule.ExecutionTimes.AddRange(times);
            return schedule;
        }

        public static ScheduleConfiguration OnDays(this ScheduleConfiguration schedule, params DayOfWeek[] days)
        {
            schedule.DaysOfWeek.AddRange(days);
            return schedule;
        }

        public static ScheduleConfiguration OnWeekdays(this ScheduleConfiguration schedule)
        {
            return schedule.OnDays(
                DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                DayOfWeek.Thursday, DayOfWeek.Friday);
        }

        public static ScheduleConfiguration OnWeekends(this ScheduleConfiguration schedule)
        {
            return schedule.OnDays(DayOfWeek.Saturday, DayOfWeek.Sunday);
        }

        public static ScheduleConfiguration Daily(this ScheduleConfiguration schedule)
        {
            schedule.Type = ScheduleType.Daily;
            return schedule;
        }

        public static ScheduleConfiguration Weekly(this ScheduleConfiguration schedule)
        {
            schedule.Type = ScheduleType.Weekly;
            return schedule;
        }

        public static ScheduleConfiguration Monthly(this ScheduleConfiguration schedule)
        {
            schedule.Type = ScheduleType.Monthly;
            return schedule;
        }

        public static ScheduleConfiguration EveryMinutes(this ScheduleConfiguration schedule, int minutes)
        {
            schedule.Type = ScheduleType.Interval;
            schedule.IntervalMinutes = minutes;
            return schedule;
        }

        public static ScheduleConfiguration Between(this ScheduleConfiguration schedule, DateTime startDate, DateTime endDate)
        {
            schedule.StartDate = startDate;
            schedule.EndDate = endDate;
            return schedule;
        }

        public static ScheduleConfiguration StartingAt(this ScheduleConfiguration schedule, DateTime startDate)
        {
            schedule.StartDate = startDate;
            return schedule;
        }

        public static ScheduleConfiguration EndingAt(this ScheduleConfiguration schedule, DateTime endDate)
        {
            schedule.EndDate = endDate;
            return schedule;
        }

        public static ScheduleConfiguration Enable(this ScheduleConfiguration schedule)
        {
            schedule.IsEnabled = true;
            return schedule;
        }

        public static ScheduleConfiguration Disable(this ScheduleConfiguration schedule)
        {
            schedule.IsEnabled = false;
            return schedule;
        }
    }
}
