using FileUtilityLib.Core.Interfaces;
using FileUtilityLib.Models.Events;
using FileUtilityLib.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileUtilityLib.Core.Services
{
    public class FileUtilityService : IFileUtilityService
    {
        private readonly IServiceScope _serviceScope;
        private readonly ILogger<FileUtilityService> _logger;
        private readonly List<PendingSchedule> _pendingSchedules; // ✅ NUEVA: Cola de schedules pendientes
        private readonly ScheduleManager _directScheduleManager;
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

        public FileUtilityService(IServiceProvider serviceProvider, string? configDirectory = null)
        {
            _serviceScope = serviceProvider.CreateScope();
            var services = _serviceScope.ServiceProvider;

            _logger = services.GetRequiredService<ILogger<FileUtilityService>>();
            _pendingSchedules = new List<PendingSchedule>(); // ✅ INICIALIZAR
            _directScheduleManager = new ScheduleManager(configDirectory);

            // Crear instancias de servicios
            TaskManager = new TaskManager(
                services.GetRequiredService<ILogger<TaskManager>>(),
                configDirectory);

            FileCopyService = new FileCopyService(
                services.GetRequiredService<ILogger<FileCopyService>>(),
                TaskManager);

            // USAR NUESTRO SCHEDULER PERSONALIZADO
            SchedulerService = new CustomSchedulerService(
                services.GetRequiredService<ILogger<CustomSchedulerService>>(),
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

        // Resto de métodos igual que antes...
        public async Task<string> CreateTaskAsync(FileCopyTask task, ScheduleConfiguration? schedule = null)
        {
            try
            {
                _logger.LogInformation("📝 Creando tarea: {TaskName}", task.Name);

                var taskId = TaskManager.AddTask(task);
                await TaskManager.SaveTasksAsync();

                _logger.LogInformation("💾 Tarea guardada con ID: {TaskId}", taskId);

                if (schedule != null)
                {
                    _logger.LogInformation("⏰ Programando tarea...");

                    schedule.TaskId = taskId;  // CRÍTICO: Asegurar que tenga el TaskId

                    // Si el scheduler ya está corriendo, programar inmediatamente
                    if (IsSchedulerRunning)
                    {
                        await SchedulerService.ScheduleTaskAsync(taskId, schedule);
                        _logger.LogInformation("✅ Tarea programada mientras scheduler está activo");
                    }
                    else
                    {
                        // ✅ SOLUCIÓN: Guardar en cola pendiente Y en archivo
                        _pendingSchedules.Add(new PendingSchedule { TaskId = taskId, Schedule = schedule });

                        // ✅ CRÍTICO: Guardar schedule aunque el scheduler no esté activo
                        //var scheduleManager = new ScheduleManager(_logger.LoggerFactory?.CreateLogger<ScheduleManager>() ??
                        //   Microsoft.Extensions.Logging.Abstractions.NullLogger<ScheduleManager>.Instance);

                        await _directScheduleManager.LoadSchedulesAsync();
                        _directScheduleManager.AddOrUpdateSchedule(schedule);
                        await _directScheduleManager.SaveSchedulesAsync();

                        _logger.LogInformation("💾 Schedule guardado para programación posterior");
                        _logger.LogInformation($"📋 Schedules pendientes: {_pendingSchedules.Count}");
                    }
                }

                _logger.LogInformation("✅ Tarea creada exitosamente: {TaskName} (ID: {TaskId})", task.Name, taskId);
                return taskId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creando tarea: {TaskName}", task.Name);
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

                // Remover de schedules pendientes si existe
                _pendingSchedules.RemoveAll(p => p.TaskId == taskId);

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
                _logger.LogInformation("🚀 Iniciando programador de tareas...");

                // PASO 1: Cargar tareas ANTES de iniciar scheduler
                await TaskManager.LoadTasksAsync();
                _logger.LogInformation("📋 Tareas cargadas desde archivo");

                // PASO 2: Procesar schedules pendientes
                if (_pendingSchedules.Count > 0)
                {
                    _logger.LogInformation($"📋 Procesando {_pendingSchedules.Count} schedules pendientes...");

                    // Los schedules pendientes ya están guardados en archivo, 
                    // pero asegurémonos de que se programen
                    foreach (var pending in _pendingSchedules)
                    {
                        _logger.LogInformation($"📅 Preparando schedule pendiente para tarea: {pending.TaskId}");
                    }
                }

                // PASO 3: Iniciar el scheduler (que cargará y programará las tareas)
                await SchedulerService.StartAsync();

                // PASO 4: Limpiar schedules pendientes ya que se procesaron
                _pendingSchedules.Clear();

                _logger.LogInformation("✅ Programador de tareas iniciado exitosamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error iniciando el programador de tareas");
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
                return await Task.FromResult(SchedulerService.GetNextExecutionTimes(taskId, count));
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
                    SchedulerService?.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deteniendo el scheduler durante dispose");
                }

                _serviceScope?.Dispose();
                _disposed = true;
            }
        }

        // ✅ NUEVA: Clase auxiliar para schedules pendientes
        private class PendingSchedule
        {
            public string TaskId { get; set; } = string.Empty;
            public ScheduleConfiguration Schedule { get; set; } = new();
        }
    }

}
