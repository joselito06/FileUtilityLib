using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSchedulerLib.Conditions
{
    public class TaskSchedulerService
    {
        private readonly List<ScheduledTask> _tasks = new();

        public void AddTask(ScheduledTask task)
        {
            _tasks.Add(task);
            _ = MonitorTask(task);
        }

        public void StopTask(string name)
        {
            var task = _tasks.FirstOrDefault(t => t.Name == name);
            task?.Stop();
        }

        public void StopAll()
        {
            foreach (var task in _tasks)
                task.Stop();
        }

        private async Task MonitorTask(ScheduledTask task)
        {
            while (!task.Cancellation.IsCancellationRequested)
            {
                var now = DateTime.Now;

                if (!task.ExcludedDays.Contains(now.DayOfWeek))
                {
                    foreach (var time in task.RunTimes)
                    {
                        if (now.Hour == time.Hours && now.Minute == time.Minutes)
                        {
                            await task.ExecuteAsync();
                        }
                    }
                }

                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), task.Cancellation.Token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }
    }
}
