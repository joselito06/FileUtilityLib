
namespace FileUtilityLib.Scheduler
{
    public class TaskSchedulerService
    {
        private readonly List<ScheduledTask> _tasks = new();

        public void AddTask(ScheduledTask task)
        {
            _tasks.Add(task);
            _ = task.StartAsync(); // iniciar en background
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
    }
}
