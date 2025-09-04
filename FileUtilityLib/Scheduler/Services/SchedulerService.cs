using FileUtilityLib.Core.Interfaces;
using FileUtilityLib.Core.Services;
using FileUtilityLib.Models.Events;
using FileUtilityLib.Models;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl;
using FileUtilityLib.Scheduler.Jobs;

namespace FileUtilityLib.Scheduler.Services
{
    public class SchedulerService : ISchedulerService
    {
        private readonly ILogger<SchedulerService> _logger;
        private readonly IFileCopyService _fileCopyService;
        private readonly ITaskManager _taskManager;
        private readonly ScheduleManager _scheduleManager;
        private IScheduler? _scheduler;

        public event EventHandler<TaskScheduleEventArgs>? TaskScheduled;
        public event EventHandler<TaskScheduleEventArgs>? TaskExecuting;

        public bool IsRunning => _scheduler?.IsStarted == true && !_scheduler.IsShutdown;

        public SchedulerService(
            ILogger<SchedulerService> logger,
            IFileCopyService fileCopyService,
            ITaskManager taskManager,
            string? configDirectory = null)
        {
            _logger = logger;
            _fileCopyService = fileCopyService;
            _taskManager = taskManager;
            _scheduleManager = new ScheduleManager(
                //logger.CreateLogger<ScheduleManager>(),
                configDirectory);
        }

        public async Task StartAsync()
        {
            if (_scheduler != null && IsRunning)
            {
                _logger.LogWarning("El programador ya está ejecutándose");
                return;
            }

            try
            {
                // Cargar configuraciones
                await _scheduleManager.LoadSchedulesAsync();

                // Crear scheduler
                var factory = new StdSchedulerFactory();
                _scheduler = await factory.GetScheduler();

                // Configurar el contexto del job
                _scheduler.Context.Put("FileCopyService", _fileCopyService);
                _scheduler.Context.Put("TaskManager", _taskManager);
                _scheduler.Context.Put("Logger", _logger);
                _scheduler.Context.Put("SchedulerService", this);

                await _scheduler.Start();

                // Programar tareas existentes
                await ScheduleExistingTasks();

                _logger.LogInformation("Servicio de programación iniciado correctamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error iniciando el servicio de programación");
                throw;
            }
        }

        public async Task StopAsync()
        {
            if (_scheduler != null)
            {
                await _scheduler.Shutdown();
                _logger.LogInformation("Servicio de programación detenido");
            }
        }

        public async Task ScheduleTaskAsync(string taskId, ScheduleConfiguration schedule)
        {
            if (_scheduler == null || !IsRunning)
            {
                throw new InvalidOperationException("El programador no está ejecutándose");
            }

            var task = _taskManager.GetTask(taskId);
            if (task == null)
            {
                throw new ArgumentException($"Tarea no encontrada: {taskId}");
            }

            try
            {
                // Eliminar programación existente si existe
                await UnscheduleTaskAsync(taskId);

                // Crear job
                var jobKey = new JobKey($"CopyJob_{taskId}", "FileCopy");
                var job = JobBuilder.Create<FileCopyJob>()
                    .WithIdentity(jobKey)
                    .UsingJobData("TaskId", taskId)
                    .Build();

                // Crear triggers según el tipo de programación
                var triggers = CreateTriggersForSchedule(taskId, schedule);

                if (triggers.Any())
                {
                    await _scheduler.ScheduleJob(job, triggers, true);

                    _scheduleManager.AddOrUpdateSchedule(schedule);
                    await _scheduleManager.SaveSchedulesAsync();

                    _logger.LogInformation("Tarea programada: {TaskId} con {TriggerCount} disparadores",
                        taskId, triggers.Count);

                    // Notificar próxima ejecución
                    var nextExecution = await GetNextExecutionTime(taskId);
                    if (nextExecution.HasValue)
                    {
                        TaskScheduled?.Invoke(this, new TaskScheduleEventArgs(
                            taskId, task.Name, nextExecution.Value, DateTime.Now));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error programando tarea: {TaskId}", taskId);
                throw;
            }
        }

        public async Task UnscheduleTaskAsync(string taskId)
        {
            if (_scheduler == null) return;

            try
            {
                var jobKey = new JobKey($"CopyJob_{taskId}", "FileCopy");
                await _scheduler.DeleteJob(jobKey);

                _scheduleManager.RemoveSchedule(taskId);
                await _scheduleManager.SaveSchedulesAsync();

                _logger.LogInformation("Tarea desprogramada: {TaskId}", taskId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error desprogramando tarea: {TaskId}", taskId);
            }
        }

        public async Task<bool> UpdateScheduleAsync(string taskId, ScheduleConfiguration schedule)
        {
            try
            {
                await ScheduleTaskAsync(taskId, schedule);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando programación para tarea: {TaskId}", taskId);
                return false;
            }
        }

        public List<ScheduleConfiguration> GetAllSchedules()
        {
            return _scheduleManager.GetAllSchedules();
        }

        public ScheduleConfiguration? GetSchedule(string taskId)
        {
            return _scheduleManager.GetSchedule(taskId);
        }

        public async Task<DateTime?> GetNextExecutionTime(string taskId)
        {
            var times = await GetNextExecutionTimesAsync(taskId, 1);
            return times.FirstOrDefault();
        }

        public List<DateTime> GetNextExecutionTimes(string taskId, int count = 5)
        {
            return _scheduleManager.GetNextExecutionTimes(taskId, count);
        }

        private async Task<List<DateTime>> GetNextExecutionTimesAsync(string taskId, int count = 5)
        {
            if (_scheduler == null || !IsRunning)
                return new List<DateTime>();

            try
            {
                var jobKey = new JobKey($"CopyJob_{taskId}", "FileCopy");
                var triggers = await _scheduler.GetTriggersOfJob(jobKey);

                var times = new List<DateTime>();

                foreach (var trigger in triggers)
                {
                    var nextFireTime = trigger.GetNextFireTimeUtc();
                    if (nextFireTime.HasValue)
                    {
                        times.Add(nextFireTime.Value.DateTime);
                    }
                }

                return times.OrderBy(t => t).Take(count).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo próximas ejecuciones para tarea: {TaskId}", taskId);
                return new List<DateTime>();
            }
        }

        private async Task ScheduleExistingTasks()
        {
            var schedules = _scheduleManager.GetEnabledSchedules();

            foreach (var schedule in schedules)
            {
                var task = _taskManager.GetTask(schedule.TaskId);
                if (task != null && task.IsEnabled)
                {
                    try
                    {
                        await ScheduleTaskAsync(schedule.TaskId, schedule);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error programando tarea existente: {TaskId}", schedule.TaskId);
                    }
                }
            }
        }

        private List<ITrigger> CreateTriggersForSchedule(string taskId, ScheduleConfiguration schedule)
        {
            var triggers = new List<ITrigger>();

            switch (schedule.Type)
            {
                case ScheduleType.Daily:
                    triggers.AddRange(CreateDailyTriggers(taskId, schedule));
                    break;

                case ScheduleType.Weekly:
                    triggers.AddRange(CreateWeeklyTriggers(taskId, schedule));
                    break;

                case ScheduleType.Monthly:
                    triggers.AddRange(CreateMonthlyTriggers(taskId, schedule));
                    break;

                case ScheduleType.Interval:
                    triggers.Add(CreateIntervalTrigger(taskId, schedule));
                    break;
            }

            return triggers;
        }

        private List<ITrigger> CreateDailyTriggers(string taskId, ScheduleConfiguration schedule)
        {
            var triggers = new List<ITrigger>();

            for (int i = 0; i < schedule.ExecutionTimes.Count; i++)
            {
                var time = schedule.ExecutionTimes[i];
                var triggerBuilder = TriggerBuilder.Create()
                    .WithIdentity($"DailyTrigger_{taskId}_{i}", "FileCopy")
                    .WithDailyTimeIntervalSchedule(x => x
                        .OnEveryDay()
                        .StartingDailyAt(TimeOfDay.HourAndMinuteOfDay(time.Hours, time.Minutes)));

                ApplyDateRange(triggerBuilder, schedule);
                triggers.Add(triggerBuilder.Build());
            }

            return triggers;
        }

        private List<ITrigger> CreateWeeklyTriggers(string taskId, ScheduleConfiguration schedule)
        {
            var triggers = new List<ITrigger>();

            foreach (var dayOfWeek in schedule.DaysOfWeek)
            {
                for (int i = 0; i < schedule.ExecutionTimes.Count; i++)
                {
                    var time = schedule.ExecutionTimes[i];
                    var triggerBuilder = TriggerBuilder.Create()
                        .WithIdentity($"WeeklyTrigger_{taskId}_{dayOfWeek}_{i}", "FileCopy")
                        .WithSchedule(CronScheduleBuilder
                            .WeeklyOnDayAndHourAndMinute(dayOfWeek, time.Hours, time.Minutes));

                    ApplyDateRange(triggerBuilder, schedule);
                    triggers.Add(triggerBuilder.Build());
                }
            }

            return triggers;
        }

        private List<ITrigger> CreateMonthlyTriggers(string taskId, ScheduleConfiguration schedule)
        {
            var triggers = new List<ITrigger>();

            for (int i = 0; i < schedule.ExecutionTimes.Count; i++)
            {
                var time = schedule.ExecutionTimes[i];
                var triggerBuilder = TriggerBuilder.Create()
                    .WithIdentity($"MonthlyTrigger_{taskId}_{i}", "FileCopy")
                    .WithSchedule(CronScheduleBuilder
                        .MonthlyOnDayAndHourAndMinute(1, time.Hours, time.Minutes)); // Primer día del mes

                ApplyDateRange(triggerBuilder, schedule);
                triggers.Add(triggerBuilder.Build());
            }

            return triggers;
        }

        private ITrigger CreateIntervalTrigger(string taskId, ScheduleConfiguration schedule)
        {
            var triggerBuilder = TriggerBuilder.Create()
                .WithIdentity($"IntervalTrigger_{taskId}", "FileCopy")
                .WithSimpleSchedule(x => x
                    .WithIntervalInMinutes(schedule.IntervalMinutes)
                    .RepeatForever());

            ApplyDateRange(triggerBuilder, schedule);
            return triggerBuilder.Build();
        }

        private static void ApplyDateRange(TriggerBuilder triggerBuilder, ScheduleConfiguration schedule)
        {
            if (schedule.StartDate.HasValue)
            {
                triggerBuilder.StartAt(schedule.StartDate.Value);
            }
            else
            {
                triggerBuilder.StartNow();
            }

            if (schedule.EndDate.HasValue)
            {
                triggerBuilder.EndAt(schedule.EndDate.Value);
            }
        }

        internal void OnTaskExecuting(string taskId, string taskName, DateTime scheduledTime)
        {
            TaskExecuting?.Invoke(this, new TaskScheduleEventArgs(taskId, taskName, scheduledTime, DateTime.Now));
        }
    }
}
