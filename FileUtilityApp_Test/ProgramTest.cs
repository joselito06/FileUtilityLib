using FileUtilityLib.Extensions;
using FileUtilityLib.Models;
using FileUtilityLib.Scheduler.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileUtilityApp_Test
{
    public class ProgramTest
    {
        static async Task Main1(string[] args)
        {
            /*Console.WriteLine("=== FileUtilityLib - Ejemplo Completo ===\n");

            // 1. Crear el servicio principal
            using var fileUtility = ServiceCollectionExtensions.CreateFileUtilityService(@"C:\FileUtilityConfig");

            // 2. Configurar eventos para monitoreo
            ConfigureEventHandlers(fileUtility);

            try
            {
                // 3. Crear diferentes tipos de tareas
                await CreateSampleTasks(fileUtility);

                // 4. Iniciar el programador
                Console.WriteLine("Iniciando programador de tareas...");
                await fileUtility.StartSchedulerAsync();

                // 5. Mostrar estado del sistema
                await ShowSystemStatus(fileUtility);

                // 6. Ejecutar una tarea manualmente para demostración
                await ExecuteTaskManually(fileUtility);

                // 7. Mantener el programa ejecutándose
                Console.WriteLine("\n=== Sistema Activo ===");
                Console.WriteLine("Presiona 'q' para salir, 's' para ver estado, 'e' para ejecutar tarea...");

                await InteractiveMode(fileUtility);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
            finally
            {
                await fileUtility.StopSchedulerAsync();
                Console.WriteLine("Sistema detenido correctamente.");
            }*/

        }

        private static void ConfigureEventHandlers(FileUtilityService2 fileUtility)
        {
            // Evento cuando inicia una operación
            fileUtility.OperationStarted += (sender, e) =>
            {
                Console.WriteLine($"🚀 INICIANDO: {e.Result.TaskName} ({e.Result.StartTime:HH:mm:ss})");
            };

            // Evento cuando termina una operación
            fileUtility.OperationCompleted += (sender, e) =>
            {
                var statusIcon = e.Result.Status switch
                {
                    CopyStatus.Completed => "✅",
                    CopyStatus.PartialSuccess => "⚠️",
                    CopyStatus.Failed => "❌",
                    _ => "ℹ️"
                };

                Console.WriteLine($"{statusIcon} COMPLETADO: {e.Result.TaskName}");
                Console.WriteLine($"   Estado: {e.Result.Status}");
                Console.WriteLine($"   Archivos: {e.Result.SuccessfulFiles}/{e.Result.TotalFiles} exitosos");
                Console.WriteLine($"   Duración: {e.Result.Duration.TotalSeconds:F1}s");

                if (!string.IsNullOrEmpty(e.Result.GeneralError))
                    Console.WriteLine($"   Error: {e.Result.GeneralError}");
            };

            // Evento para cada archivo procesado
            fileUtility.FileProcessed += (sender, e) =>
            {
                var statusIcon = e.Result.Success ? "✓" : "✗";
                var fileName = System.IO.Path.GetFileName(e.Result.FilePath);
                Console.WriteLine($"   {statusIcon} {fileName} ({e.Result.FileSizeBytes / 1024}KB)");

                if (!e.Result.Success && !string.IsNullOrEmpty(e.Result.ErrorMessage))
                    Console.WriteLine($"     Error: {e.Result.ErrorMessage}");
            };

            // Evento cuando se ejecuta una tarea programada
            fileUtility.TaskExecuting += (sender, e) =>
            {
                Console.WriteLine($"⏰ PROGRAMADA: {e.TaskName} (programada: {e.ScheduledTime:HH:mm:ss})");
            };
        }

        private static async Task CreateSampleTasks(FileUtilityService2 fileUtility)
        {
            Console.WriteLine("Creando tareas de ejemplo...\n");

            // Tarea 1: Backup diario de documentos modificados hoy
            var documentBackup = new FileCopyTask
            {
                Name = "Backup Documentos Diario",
                SourcePath = @"C:\Users\Documents"  // Cambia por tu ruta real
            }
            .AddDestination(@"D:\Backup\Documents")
            .AddFilePatterns("*.docx", "*.pdf", "*.xlsx", "*.txt")
            .ModifiedToday()  // Solo archivos modificados hoy
            .Enable();

            var dailySchedule = new ScheduleConfiguration()
                .Daily()
                .AddExecutionTime(8, 0)   // 8:00 AM
                .AddExecutionTime(17, 0)  // 5:00 PM
                .OnWeekdays()             // Solo días laborales
                .Enable();

            var task1Id = await fileUtility.CreateTaskAsync(documentBackup, dailySchedule);
            Console.WriteLine($"✅ Tarea creada: Backup Documentos ({task1Id})");

            // Tarea 2: Logs de aplicación cada 2 horas
            var logBackup = new FileCopyTask
            {
                Name = "Backup Logs Aplicación",
                SourcePath = @"C:\Logs\MyApp"
            }
            .AddDestination(@"\\Server\Logs\Backup")
            .AddFilePattern("*.log")
            .ModifiedSince(DateTime.Now.AddHours(-3))  // Últimas 3 horas
            .FileSizeGreaterThan(1024)  // Mayor a 1KB
            .Enable();

            var intervalSchedule = new ScheduleConfiguration()
                .EveryMinutes(120)  // Cada 2 horas
                .StartingAt(DateTime.Now.AddMinutes(5))
                .Enable();

            var task2Id = await fileUtility.CreateTaskAsync(logBackup, intervalSchedule);
            Console.WriteLine($"✅ Tarea creada: Backup Logs ({task2Id})");

            // Tarea 3: Archivos grandes semanales
            var weeklyArchive = new FileCopyTask
            {
                Name = "Archivo Semanal Archivos Grandes",
                SourcePath = @"C:\Data\Processing"
            }
            .AddDestinations(@"\\Archive\Weekly", @"\\Backup\Archive")
            .AddFilePatterns("*.zip", "*.rar", "*.7z", "*.tar")
            .ModifiedSince(DateTime.Today.AddDays(-7))  // Última semana
            .FileSizeGreaterThan(50 * 1024 * 1024)     // Mayor a 50MB
            .Enable();

            var weeklySchedule = new ScheduleConfiguration()
                .Weekly()
                .OnDays(DayOfWeek.Sunday)    // Solo domingos
                .AddExecutionTime(3, 0)      // 3:00 AM
                .Enable();

            var task3Id = await fileUtility.CreateTaskAsync(weeklyArchive, weeklySchedule);
            Console.WriteLine($"✅ Tarea creada: Archivo Semanal ({task3Id})");

            Console.WriteLine();
        }

        private static async Task ShowSystemStatus(FileUtilityService2 fileUtility)
        {
            Console.WriteLine("=== ESTADO DEL SISTEMA ===");

            var tasks = fileUtility.GetAllTasks();
            var schedules = fileUtility.GetAllSchedules();

            Console.WriteLine($"📋 Total de tareas: {tasks.Count}");
            Console.WriteLine($"⏰ Tareas programadas: {schedules.Count(s => s.IsEnabled)}");
            Console.WriteLine($"🟢 Programador activo: {(fileUtility.IsSchedulerRunning ? "Sí" : "No")}");

            Console.WriteLine("\n=== PRÓXIMAS EJECUCIONES ===");
            foreach (var task in tasks.Where(t => t.IsEnabled))
            {
                var nextExecutions = await fileUtility.GetNextExecutionTimesAsync(task.Id, 3);
                Console.WriteLine($"\n📁 {task.Name}:");

                if (nextExecutions.Count > 0)
                {
                    foreach (var next in nextExecutions)
                    {
                        var timeUntil = next - DateTime.Now;
                        Console.WriteLine($"   ⏰ {next:ddd dd/MM/yyyy HH:mm} (en {timeUntil.TotalHours:F1}h)");
                    }
                }
                else
                {
                    Console.WriteLine("   ❌ Sin programación activa");
                }

                if (task.LastExecuted.HasValue)
                {
                    Console.WriteLine($"   📅 Última ejecución: {task.LastExecuted:dd/MM/yyyy HH:mm:ss}");
                }
            }
            Console.WriteLine();
        }

        private static async Task ExecuteTaskManually(FileUtilityService2 fileUtility)
        {
            var tasks = fileUtility.GetAllTasks();
            if (!tasks.Any())
            {
                Console.WriteLine("❌ No hay tareas disponibles para ejecutar");
                return;
            }

            var firstTask = tasks.First();
            Console.WriteLine($"🔧 Ejecutando manualmente: {firstTask.Name}");
            Console.WriteLine("   (Esta es solo una demostración - ajusta las rutas en el código)");

            try
            {
                var result = await fileUtility.ExecuteTaskNowAsync(firstTask.Id);

                Console.WriteLine($"✅ Ejecución manual completada:");
                Console.WriteLine($"   Estado: {result.Status}");
                Console.WriteLine($"   Archivos encontrados: {result.TotalFiles}");
                Console.WriteLine($"   Archivos copiados: {result.SuccessfulFiles}");
                Console.WriteLine($"   Errores: {result.FailedFiles}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en ejecución manual: {ex.Message}");
            }
        }

        private static async Task InteractiveMode(FileUtilityService2 fileUtility)
        {
            while (true)
            {
                var key = Console.ReadKey(true);

                switch (key.KeyChar)
                {
                    case 'q':
                    case 'Q':
                        return;

                    case 's':
                    case 'S':
                        await ShowSystemStatus(fileUtility);
                        break;

                    case 'e':
                    case 'E':
                        await ExecuteTaskManually(fileUtility);
                        break;

                    case 't':
                    case 'T':
                        await ShowTaskDetails(fileUtility);
                        break;

                    default:
                        Console.WriteLine("\n❓ Comandos disponibles:");
                        Console.WriteLine("   's' - Ver estado del sistema");
                        Console.WriteLine("   'e' - Ejecutar tarea manualmente");
                        Console.WriteLine("   't' - Ver detalles de tareas");
                        Console.WriteLine("   'q' - Salir");
                        break;
                }
            }
        }

        private static async Task ShowTaskDetails(FileUtilityService2 fileUtility)
        {
            Console.WriteLine("\n=== DETALLES DE TAREAS ===");

            var tasks = fileUtility.GetAllTasks();

            for (int i = 0; i < tasks.Count; i++)
            {
                var task = tasks[i];
                Console.WriteLine($"\n{i + 1}. 📁 {task.Name} ({task.Id})");
                Console.WriteLine($"   📂 Origen: {task.SourcePath}");
                Console.WriteLine($"   📤 Destinos: {string.Join(", ", task.DestinationPaths)}");
                Console.WriteLine($"   🏷️ Patrones: {string.Join(", ", task.FilePatterns.DefaultIfEmpty("*.*"))}");
                Console.WriteLine($"   🟢 Habilitada: {task.IsEnabled}");
                Console.WriteLine($"   📊 Condiciones: {task.Conditions.Count}");

                foreach (var condition in task.Conditions)
                {
                    Console.WriteLine($"      - {condition.Type}: {condition.Value}");
                }

                if (task.LastExecuted.HasValue)
                {
                    Console.WriteLine($"   📅 Última ejecución: {task.LastExecuted:dd/MM/yyyy HH:mm:ss}");
                }

                var schedule = fileUtility.GetAllSchedules().FirstOrDefault(s => s.TaskId == task.Id);
                if (schedule != null && schedule.IsEnabled)
                {
                    Console.WriteLine($"   ⏰ Programación: {schedule.Type}");
                    Console.WriteLine($"      Horarios: {string.Join(", ", schedule.ExecutionTimes.Select(t => t.ToString(@"hh\:mm")))}");

                    if (schedule.DaysOfWeek.Any())
                        Console.WriteLine($"      Días: {string.Join(", ", schedule.DaysOfWeek)}");
                }
            }
        }
    }
}
