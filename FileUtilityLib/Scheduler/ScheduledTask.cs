using FileUtilityLib.Core;

namespace FileUtilityLib.Scheduler
{
    public class ScheduledTask
    {
        public string Name { get; set; } = string.Empty;
        public IFileTask TaskAction { get; set; }
        public List<TimeSpan> ExecutionTimes { get; set; } = new();
        public List<DayOfWeek> ExcludedDays { get; set; } = new();
        public CancellationTokenSource Cancellation { get; private set; } = new();

        public async Task StartAsync()
        {
            while (!Cancellation.IsCancellationRequested)
            {
                var now = DateTime.Now;

                if (!ExcludedDays.Contains(now.DayOfWeek))
                {
                    var match = ExecutionTimes.FirstOrDefault(t =>
                        now.Hour == t.Hours && now.Minute == t.Minutes);

                    if (match != default)
                    {
                        await TaskAction.ExecuteAsync(Cancellation.Token);
                    }
                }

                // Revisa cada minuto
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), Cancellation.Token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        public void Stop() => Cancellation.Cancel();
    }
}
