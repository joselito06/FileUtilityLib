using FileUtilityLib.Core.Interfaces;
using FileUtilityLib.Scheduler.Services;
using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileUtilityLib.Scheduler.Jobs
{
    [DisallowConcurrentExecution]
    public class FileCopyJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            var taskId = context.JobDetail.JobDataMap.GetString("TaskId");
            var taskName = context.JobDetail.JobDataMap.GetString("TaskName") ?? "Unknown";

            if (string.IsNullOrEmpty(taskId))
            {
                throw new InvalidOperationException("TaskId no especificado en JobDataMap");
            }

            // Obtener servicios del contexto
            var fileCopyService = (IFileCopyService?)context.Scheduler.Context.Get("FileCopyService");
            var taskManager = (ITaskManager?)context.Scheduler.Context.Get("TaskManager");
            var logger = (ILogger?)context.Scheduler.Context.Get("Logger");
            var schedulerService = (SchedulerService?)context.Scheduler.Context.Get("SchedulerService");

            if (fileCopyService == null || taskManager == null || logger == null)
            {
                throw new InvalidOperationException("Servicios requeridos no encontrados en el contexto del scheduler");
            }

            var task = taskManager.GetTask(taskId);
            if (task == null)
            {
                logger.LogWarning("Tarea programada no encontrada: {TaskId}", taskId);
                return;
            }

            if (!task.IsEnabled)
            {
                logger.LogInformation("Tarea deshabilitada, omitiendo ejecución: {TaskName}", task.Name);
                return;
            }

            try
            {
                logger.LogInformation("🔥 EJECUTANDO TAREA PROGRAMADA: {TaskName} (ID: {TaskId})", task.Name, taskId);

                // Notificar inicio de ejecución
                var scheduledTime = context.ScheduledFireTimeUtc?.DateTime ?? DateTime.Now;
                schedulerService?.OnTaskExecuting(taskId, task.Name, scheduledTime);

                var result = await fileCopyService.ExecuteTaskAsync(task, context.CancellationToken);

                logger.LogInformation(
                    "✅ TAREA PROGRAMADA COMPLETADA: {TaskName}. Estado: {Status}, Exitosos: {Success}, Fallidos: {Failed}",
                    task.Name, result.Status, result.SuccessfulFiles, result.FailedFiles);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ ERROR ejecutando tarea programada: {TaskName}", task.Name);
                throw;
            }
        }
    }
}
