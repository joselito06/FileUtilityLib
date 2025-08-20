using FileSchedulerLib.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSchedulerLib.Conditions
{
    public class ScheduledTask
    {
        public required string Name { get; set; }
        public required Func<Task> TaskAction { get; set; }
        public List<TimeSpan> RunTimes { get; set; } = new();
        public List<DayOfWeek> ExcludedDays { get; set; } = new();
        public List<ITaskCondition> Conditions { get; set; } = new();
        public CancellationTokenSource Cancellation { get; private set; } = new();

        public async Task ExecuteAsync()
        {
            // Validar condiciones
            if (Conditions.Any(c => !c.ShouldRun()))
                return;

            await TaskAction();
        }

        public void Stop() => Cancellation.Cancel();
    }
}
