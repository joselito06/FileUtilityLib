using FileUtilityLib.Core.Interfaces;
using FileUtilityLib.Models.Events;
using FileUtilityLib.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileUtilityLib.Core.Services
{
    public class CustomSchedulerService : ISchedulerService
    {
        private readonly ILogger<CustomSchedulerService> _logger;
        private readonly IFileCopyService _fileCopyService;
        private readonly ITaskManager _taskManager;
        private readonly ScheduleManager _scheduleManager;
        private readonly ConcurrentDictionary<string, ScheduledTaskInfo> _scheduledTasks;
        private readonly Timer _schedulerTimer;
        private readonly object _lock = new();
        private bool _isRunning;
        private bool _disposed;

        public event EventHandler<TaskScheduleEventArgs>? TaskScheduled;
        public event EventHandler<TaskScheduleEventArgs>? TaskExecuting;

        public bool IsRunning => _isRunning;

        public CustomSchedulerService(
            ILogger<CustomSchedulerService> logger,
            IFileCopyService fileCopyService,
            ITaskManager taskManager,
            string? configDirectory = null)
        {
            _logger = logger;
            _fileCopyService = fileCopyService;
            _taskManager = taskManager;
            _scheduleManager = new ScheduleManager(configDirectory);
            _scheduledTasks = new ConcurrentDictionary<string, ScheduledTaskInfo>();

            // Timer que revisa cada 30 segundos si hay tareas que ejecutar
            _schedulerTimer = new Timer(CheckAndExecuteTasks, null, Timeout.Infinite, Timeout.Infinite);
        }

        public async Task StartAsync()
        {
            if (_isRunning)
            {
                _logger.LogWarning("El scheduler ya está ejecutándose");
                return;
            }

            try
            {
                _logger.LogInformation("Iniciando scheduler personalizado...");

                // Cargar schedules existentes
                await _scheduleManager.LoadSchedulesAsync();
                _logger.LogInformation("📋 Schedules cargados desde archivo");

                // Programar tareas existentes
                await ScheduleExistingTasks();

                // Iniciar el timer (revisar cada 30 segundos)
                _schedulerTimer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(30));

                _isRunning = true;
                _logger.LogInformation("✅ Scheduler personalizado iniciado correctamente");
                _logger.LogInformation($"📊 Tareas programadas activas: {_scheduledTasks.Count}");

                // Debug: Mostrar tareas programadas
                foreach (var kvp in _scheduledTasks)
                {
                    var task = kvp.Value;
                    var nextExecution = task.NextExecutions.FirstOrDefault();
                    _logger.LogInformation($"   📅 {task.TaskName}: próxima ejecución {nextExecution:yyyy-MM-dd HH:mm:ss}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error iniciando el scheduler personalizado");
                throw;
            }
        }

        public async Task StopAsync()
        {
            if (!_isRunning)
                return;

            try
            {
                _logger.LogInformation("Deteniendo scheduler personalizado...");

                _isRunning = false;
                _schedulerTimer.Change(Timeout.Infinite, Timeout.Infinite);

                // Esperar que terminen las tareas en ejecución
                await Task.Delay(1000);

                _logger.LogInformation("✅ Scheduler personalizado detenido");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error deteniendo el scheduler");
            }
        }

        public async Task ScheduleTaskAsync(string taskId, ScheduleConfiguration schedule)
        {
            try
            {
                var task = _taskManager.GetTask(taskId);
                if (task == null)
                {
                    throw new ArgumentException($"Tarea no encontrada: {taskId}");
                }

                _logger.LogInformation("📅 Programando tarea: {TaskName} (ID: {TaskId})", task.Name, taskId);

                // IMPORTANTE: Asegurar que el schedule tenga el TaskId
                schedule.TaskId = taskId;

                // Calcular próximas ejecuciones
                var nextExecutions = CalculateNextExecutions(schedule, 10);

                if (!nextExecutions.Any())
                {
                    _logger.LogWarning("⚠️ No se pudieron calcular ejecuciones para la tarea: {TaskName}", task.Name);
                    return;
                }

                var scheduledTask = new ScheduledTaskInfo
                {
                    TaskId = taskId,
                    TaskName = task.Name,
                    Schedule = schedule,
                    NextExecutions = new Queue<DateTime>(nextExecutions),
                    LastExecuted = null,
                    IsExecuting = false
                };

                _scheduledTasks.AddOrUpdate(taskId, scheduledTask, (key, old) => scheduledTask);

                // Guardar configuración
                _scheduleManager.AddOrUpdateSchedule(schedule);
                await _scheduleManager.SaveSchedulesAsync();

                var nextExecution = nextExecutions.First();
                _logger.LogInformation("⏰ Tarea programada: {TaskName} - Próxima ejecución: {NextTime}",
                    task.Name, nextExecution.ToString("yyyy-MM-dd HH:mm:ss"));

                // Debug adicional
                _logger.LogInformation($"🔍 DEBUG: _scheduledTasks ahora tiene {_scheduledTasks.Count} tareas");
                _logger.LogInformation($"🔍 DEBUG: Próximas 3 ejecuciones: {string.Join(", ", nextExecutions.Take(3).Select(d => d.ToString("HH:mm:ss")))}");

                TaskScheduled?.Invoke(this, new TaskScheduleEventArgs(
                    taskId, task.Name, nextExecution, DateTime.Now));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error programando tarea: {TaskId}", taskId);
                throw;
            }
        }

        public async Task UnscheduleTaskAsync(string taskId)
        {
            try
            {
                if (_scheduledTasks.TryRemove(taskId, out var scheduledTask))
                {
                    _scheduleManager.RemoveSchedule(taskId);
                    await _scheduleManager.SaveSchedulesAsync();

                    _logger.LogInformation("🗑️ Tarea desprogramada: {TaskName}", scheduledTask.TaskName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error desprogramando tarea: {TaskId}", taskId);
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
                _logger.LogError(ex, "❌ Error actualizando schedule: {TaskId}", taskId);
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
            if (_scheduledTasks.TryGetValue(taskId, out var scheduledTask))
            {
                return await Task.FromResult(scheduledTask.NextExecutions.FirstOrDefault());
            }
            return null;
        }

        public List<DateTime> GetNextExecutionTimes(string taskId, int count = 5)
        {
            if (_scheduledTasks.TryGetValue(taskId, out var scheduledTask))
            {
                return scheduledTask.NextExecutions.Take(count).ToList();
            }
            return new List<DateTime>();
        }

        private async void CheckAndExecuteTasks(object? state)
        {
            if (!_isRunning || _disposed)
                return;

            try
            {
                var now = DateTime.Now;

                // DEBUG: Log cada verificación
                _logger.LogDebug($"🔍 Verificando tareas programadas... ({now:HH:mm:ss})");
                _logger.LogDebug($"🔍 Tareas en memoria: {_scheduledTasks.Count}");

                var tasksToExecute = new List<ScheduledTaskInfo>();

                // Buscar tareas que deben ejecutarse
                foreach (var kvp in _scheduledTasks)
                {
                    var scheduledTask = kvp.Value;

                    _logger.LogDebug($"🔍 Revisando tarea: {scheduledTask.TaskName}");
                    _logger.LogDebug($"   IsExecuting: {scheduledTask.IsExecuting}");
                    _logger.LogDebug($"   IsEnabled: {scheduledTask.Schedule.IsEnabled}");
                    _logger.LogDebug($"   NextExecutions count: {scheduledTask.NextExecutions.Count}");

                    if (scheduledTask.IsExecuting || !scheduledTask.Schedule.IsEnabled)
                    {
                        _logger.LogDebug($"   ⏭️ Saltando (ejecutando: {scheduledTask.IsExecuting}, habilitada: {scheduledTask.Schedule.IsEnabled})");
                        continue;
                    }

                    // Verificar si es hora de ejecutar
                    if (scheduledTask.NextExecutions.Count > 0)
                    {
                        var nextExecution = scheduledTask.NextExecutions.Peek();
                        var timeUntil = nextExecution - now;

                        _logger.LogDebug($"   Próxima ejecución: {nextExecution:HH:mm:ss} (en {timeUntil.TotalSeconds:F0}s)");

                        if (nextExecution <= now)
                        {
                            _logger.LogInformation($"⏰ ¡Es hora de ejecutar! {scheduledTask.TaskName}");
                            tasksToExecute.Add(scheduledTask);
                        }
                    }
                    else
                    {
                        _logger.LogDebug($"   ⚠️ Sin próximas ejecuciones programadas");
                    }
                }

                _logger.LogDebug($"🔍 Tareas a ejecutar: {tasksToExecute.Count}");

                // Ejecutar tareas encontradas
                foreach (var scheduledTask in tasksToExecute)
                {
                    _logger.LogInformation($"🚀 Iniciando ejecución de: {scheduledTask.TaskName}");
                    _ = Task.Run(async () => await ExecuteScheduledTask(scheduledTask));
                }

                // Actualizar próximas ejecuciones para tareas que se agotaron
                await RefreshNextExecutions();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error en CheckAndExecuteTasks");
            }
        }

        private async Task ExecuteScheduledTask(ScheduledTaskInfo scheduledTask)
        {
            try
            {
                scheduledTask.IsExecuting = true;

                // Remover la ejecución actual de la cola
                DateTime currentExecution = DateTime.Now;
                if (scheduledTask.NextExecutions.Count > 0)
                {
                    currentExecution = scheduledTask.NextExecutions.Dequeue();
                }

                _logger.LogInformation("🔥 EJECUTANDO TAREA PROGRAMADA: {TaskName} (programada para: {Time})",
                    scheduledTask.TaskName, currentExecution.ToString("HH:mm:ss"));

                TaskExecuting?.Invoke(this, new TaskScheduleEventArgs(
                    scheduledTask.TaskId, scheduledTask.TaskName, currentExecution, DateTime.Now));

                // Ejecutar la tarea
                var result = await _fileCopyService.ExecuteTaskAsync(scheduledTask.TaskId);

                scheduledTask.LastExecuted = DateTime.Now;

                _logger.LogInformation("✅ TAREA PROGRAMADA COMPLETADA: {TaskName} - Estado: {Status}, Archivos: {Files}",
                    scheduledTask.TaskName, result.Status, $"{result.SuccessfulFiles}/{result.TotalFiles}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error ejecutando tarea programada: {TaskName}", scheduledTask.TaskName);
            }
            finally
            {
                scheduledTask.IsExecuting = false;
            }
        }

        private async Task RefreshNextExecutions()
        {
            await Task.Run(() => {
                foreach (var kvp in _scheduledTasks.ToList())
                {
                    var scheduledTask = kvp.Value;

                    // Si quedan pocas ejecuciones, generar más
                    if (scheduledTask.NextExecutions.Count < 3)
                    {
                        _logger.LogDebug($"🔄 Regenerando ejecuciones para: {scheduledTask.TaskName}");
                        var newExecutions = CalculateNextExecutions(scheduledTask.Schedule, 10);

                        // Agregar solo las futuras
                        var now = DateTime.Now;
                        var existingTimes = scheduledTask.NextExecutions.ToHashSet();

                        foreach (var execution in newExecutions)
                        {
                            if (execution > now && !existingTimes.Contains(execution))
                            {
                                scheduledTask.NextExecutions.Enqueue(execution);
                            }
                        }

                        _logger.LogDebug($"🔄 Ahora tiene {scheduledTask.NextExecutions.Count} ejecuciones futuras");
                    }
                }
            });
            
        }

        private async Task ScheduleExistingTasks()
        {
            var schedules = _scheduleManager.GetEnabledSchedules();
            _logger.LogInformation("📋 Programando {Count} tareas existentes", schedules.Count);

            await Task.Run(() => {
                foreach (var schedule in schedules)
                {
                    var task = _taskManager.GetTask(schedule.TaskId);
                    if (task != null && task.IsEnabled)
                    {
                        try
                        {
                            _logger.LogInformation($"🔄 Reprogramando tarea existente: {task.Name}");

                            // No llamar ScheduleTaskAsync para evitar doble guardado
                            var nextExecutions = CalculateNextExecutions(schedule, 10);

                            if (nextExecutions.Any())
                            {
                                var scheduledTask = new ScheduledTaskInfo
                                {
                                    TaskId = schedule.TaskId,
                                    TaskName = task.Name,
                                    Schedule = schedule,
                                    NextExecutions = new Queue<DateTime>(nextExecutions),
                                    LastExecuted = null,
                                    IsExecuting = false
                                };

                                _scheduledTasks.AddOrUpdate(schedule.TaskId, scheduledTask, (key, old) => scheduledTask);

                                _logger.LogInformation("✅ Tarea reprogramada: {TaskName}", task.Name);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "❌ Error programando tarea existente: {TaskId}", schedule.TaskId);
                        }
                    }
                }
            });
            
        }

        private List<DateTime> CalculateNextExecutions(ScheduleConfiguration schedule, int count)
        {
            var executions = new List<DateTime>();
            var current = DateTime.Now;

            _logger.LogDebug($"🧮 Calculando ejecuciones para tipo: {schedule.Type}");

            switch (schedule.Type)
            {
                case ScheduleType.Interval:
                    _logger.LogDebug($"🧮 Intervalo: {schedule.IntervalMinutes} minutos");
                    for (int i = 0; i < count; i++)
                    {
                        current = current.AddMinutes(schedule.IntervalMinutes);
                        executions.Add(current);
                        _logger.LogDebug($"   {i + 1}: {current:yyyy-MM-dd HH:mm:ss}");
                    }
                    break;

                case ScheduleType.Daily:
                    var today = DateTime.Today;
                    for (int day = 0; day < count && executions.Count < count; day++)
                    {
                        var targetDate = today.AddDays(day);

                        foreach (var time in schedule.ExecutionTimes)
                        {
                            var execution = targetDate.Add(time);
                            if (execution > DateTime.Now)
                            {
                                executions.Add(execution);
                                if (executions.Count >= count) break;
                            }
                        }
                    }
                    break;

                case ScheduleType.Weekly:
                    var currentDate = DateTime.Today;
                    var daysChecked = 0;

                    while (executions.Count < count && daysChecked < 365) // Máximo un año
                    {
                        if (schedule.DaysOfWeek.Contains(currentDate.DayOfWeek))
                        {
                            foreach (var time in schedule.ExecutionTimes)
                            {
                                var execution = currentDate.Add(time);
                                if (execution > DateTime.Now)
                                {
                                    executions.Add(execution);
                                    if (executions.Count >= count) break;
                                }
                            }
                        }
                        currentDate = currentDate.AddDays(1);
                        daysChecked++;
                    }
                    break;

                case ScheduleType.Monthly:
                    var currentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                    for (int month = 0; month < count; month++)
                    {
                        var targetMonth = currentMonth.AddMonths(month);

                        foreach (var time in schedule.ExecutionTimes)
                        {
                            var execution = targetMonth.Add(time);
                            if (execution > DateTime.Now)
                            {
                                executions.Add(execution);
                            }
                        }
                    }
                    break;
            }

            var result = executions.OrderBy(e => e).Take(count).ToList();
            _logger.LogDebug($"🧮 Calculadas {result.Count} ejecuciones futuras");

            return result;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _schedulerTimer?.Dispose();
                _ = StopAsync(); // Fire and forget
            }
        }
    }

    // Clase auxiliar para información de tareas programadas
    internal class ScheduledTaskInfo
    {
        public string TaskId { get; set; } = string.Empty;
        public string TaskName { get; set; } = string.Empty;
        public ScheduleConfiguration Schedule { get; set; } = new();
        public Queue<DateTime> NextExecutions { get; set; } = new();
        public DateTime? LastExecuted { get; set; }
        public bool IsExecuting { get; set; }
    }
}
