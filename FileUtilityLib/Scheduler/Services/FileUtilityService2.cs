using FileUtilityLib.Core.Interfaces;
using FileUtilityLib.Core.Services;
using FileUtilityLib.Models.Events;
using FileUtilityLib.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FileUtilityLib.Scheduler.Services
{
    public class FileUtilityService2 : IFileUtilityService
    {
        private readonly IServiceScope _serviceScope;
        private readonly ILogger<FileUtilityService2> _logger;
        private bool _disposed;

        public IFileCopyService FileCopyService { get; }
        public ITaskManager TaskManager { get; }
        public ISchedulerService SchedulerService { get; }

        // Eventos unificados
        public event EventHandler<CopyOperationEventArgs>? OperationStarted;
        public event EventHandler<CopyOperationEventArgs>? OperationCompleted;
        public event EventHandler<FileOperationEventArgs>? FileProcessing;
        public event EventHandler<FileOperationEventArgs>? FileProcessed;
        public event EventHandler<TaskScheduleEventArgs>? TaskScheduled;
        public event EventHandler<TaskScheduleEventArgs>? TaskExecuting;

        public bool IsSchedulerRunning => SchedulerService.IsRunning;

        public FileUtilityService2(IServiceProvider serviceProvider, string? configDirectory = null)
        {
            _serviceScope = serviceProvider.CreateScope();
            var services = _serviceScope.ServiceProvider;

            _logger = services.GetRequiredService<ILogger<FileUtilityService2>>();

            // Crear instancias de servicios
            TaskManager = new TaskManager(
                services.GetRequiredService<ILogger<TaskManager>>(),
                configDirectory);

            FileCopyService = new FileCopyService(
                services.GetRequiredService<ILogger<FileCopyService>>(),
                TaskManager);

            SchedulerService = new SchedulerService(
                services.GetRequiredService<ILogger<SchedulerService>>(),
                FileCopyService,
                TaskManager,
                configDirectory);

            // Suscribirse a eventos de servicios
            SubscribeToEvents();
        }

        private void SubscribeToEvents()
        {
            // Eventos del servicio de copia
            FileCopyService.OperationStarted += (sender, e) => OperationStarted?.Invoke(this, e);
            FileCopyService.OperationCompleted += (sender, e) => OperationCompleted?.Invoke(this, e);
            FileCopyService.FileProcessing += (sender, e) => FileProcessing?.Invoke(this, e);
            FileCopyService.FileProcessed += (sender, e) => FileProcessed?.Invoke(this, e);

            // Eventos del scheduler
            SchedulerService.TaskScheduled += (sender, e) => TaskScheduled?.Invoke(this, e);
            SchedulerService.TaskExecuting += (sender, e) => TaskExecuting?.Invoke(this, e);
        }

        public async Task<string> CreateTaskAsync(FileCopyTask task, ScheduleConfiguration? schedule = null)
        {
            try
            {
                var taskId = TaskManager.AddTask(task);
                await TaskManager.SaveTasksAsync();

                if (schedule != null)
                {
                    schedule.TaskId = taskId;
                    if (IsSchedulerRunning)
                    {
                        await SchedulerService.ScheduleTaskAsync(taskId, schedule);
                    }
                }

                _logger.LogInformation("Tarea creada exitosamente: {TaskName} (ID: {TaskId})", task.Name, taskId);
                return taskId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando tarea: {TaskName}", task.Name);
                throw;
            }
        }

        public async Task<bool> UpdateTaskAsync(FileCopyTask task, ScheduleConfiguration? schedule = null)
        {
            try
            {
                var updated = TaskManager.UpdateTask(task);
                if (updated)
                {
                    await TaskManager.SaveTasksAsync();

                    if (schedule != null && IsSchedulerRunning)
                    {
                        schedule.TaskId = task.Id;
                        await SchedulerService.UpdateScheduleAsync(task.Id, schedule);
                    }

                    _logger.LogInformation("Tarea actualizada exitosamente: {TaskName} (ID: {TaskId})", task.Name, task.Id);
                }

                return updated;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando tarea: {TaskName}", task.Name);
                throw;
            }
        }

        public async Task<bool> DeleteTaskAsync(string taskId)
        {
            try
            {
                var task = TaskManager.GetTask(taskId);
                if (task == null)
                {
                    return false;
                }

                // Desprogramar la tarea si está programada
                if (IsSchedulerRunning)
                {
                    await SchedulerService.UnscheduleTaskAsync(taskId);
                }

                var removed = TaskManager.RemoveTask(taskId);
                if (removed)
                {
                    await TaskManager.SaveTasksAsync();
                    _logger.LogInformation("Tarea eliminada exitosamente: {TaskName} (ID: {TaskId})", task.Name, taskId);
                }

                return removed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando tarea: {TaskId}", taskId);
                throw;
            }
        }

        public async Task<CopyOperationResult> ExecuteTaskNowAsync(string taskId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await FileCopyService.ExecuteTaskAsync(taskId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ejecutando tarea inmediatamente: {TaskId}", taskId);
                throw;
            }
        }

        public async Task StartSchedulerAsync()
        {
            try
            {
                await TaskManager.LoadTasksAsync();
                await SchedulerService.StartAsync();
                _logger.LogInformation("Programador de tareas iniciado exitosamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error iniciando el programador de tareas");
                throw;
            }
        }

        public async Task StopSchedulerAsync()
        {
            try
            {
                await SchedulerService.StopAsync();
                _logger.LogInformation("Programador de tareas detenido exitosamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deteniendo el programador de tareas");
                throw;
            }
        }

        public List<FileCopyTask> GetAllTasks()
        {
            return TaskManager.GetAllTasks();
        }

        public List<ScheduleConfiguration> GetAllSchedules()
        {
            return SchedulerService.GetAllSchedules();
        }

        public async Task<List<DateTime>> GetNextExecutionTimesAsync(string taskId, int count = 5)
        {
            try
            {
                var nextTime = await SchedulerService.GetNextExecutionTime(taskId);
                if (nextTime.HasValue)
                {
                    return new List<DateTime> { nextTime.Value };
                }

                // Fallback a cálculo manual si el scheduler no está activo
                return SchedulerService.GetNextExecutionTimes(taskId, count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo próximas ejecuciones para tarea: {TaskId}", taskId);
                return new List<DateTime>();
            }
        }

        public async Task SaveConfigurationAsync()
        {
            try
            {
                await TaskManager.SaveTasksAsync();
                _logger.LogDebug("Configuración guardada exitosamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error guardando configuración");
                throw;
            }
        }

        public async Task LoadConfigurationAsync()
        {
            try
            {
                await TaskManager.LoadTasksAsync();
                _logger.LogDebug("Configuración cargada exitosamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cargando configuración");
                throw;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    if (IsSchedulerRunning)
                    {
                        SchedulerService.StopAsync().GetAwaiter().GetResult();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deteniendo el scheduler durante dispose");
                }

                _serviceScope?.Dispose();
                _disposed = true;
            }
        }
    }
}
