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
    public class ProgramTest2
    {
        static async Task Main2(string[] args)
        {
            /*Console.WriteLine("=== FileUtilityLib - Ejemplo FUNCIONAL ===\n");

            // PASO 1: Crear entorno de prueba con archivos REALES
            //await SetupRealTestEnvironment();

            using var fileUtility = ServiceCollectionExtensions.CreateFileUtilityService(@"C:\FileUtilityTest\Config");

            // Configurar eventos
            ConfigureEvents(fileUtility);

            try
            {
                // PASO 2: Crear tareas que SÍ funcionen
                await CreateWorkingTasks(fileUtility);

                // PASO 3: Probar ejecución manual primero
                await TestManualExecution(fileUtility);

                // PASO 4: Iniciar scheduler DESPUÉS de crear tareas
                Console.WriteLine("\nIniciando programador de tareas...");
                await fileUtility.StartSchedulerAsync();

                // PASO 5: Mostrar estado
                await ShowDetailedStatus(fileUtility);

                // PASO 6: Esperar ejecuciones programadas
                Console.WriteLine("\n=== Esperando Ejecuciones Programadas ===");
                Console.WriteLine("Las tareas se ejecutarán automáticamente...");
                Console.WriteLine("Presiona 'q' para salir, 's' para estado, 'e' para ejecutar manual...");

                await InteractiveMode(fileUtility);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
            }*/
        }

        private static async Task SetupRealTestEnvironment()
        {
            Console.WriteLine("🔧 Creando entorno de prueba REAL...");

            var sourceDir = @"C:\FileUtilityTest\Source";
            var destDir = @"C:\FileUtilityTest\Destination\AllFiles";

            // Crear directorios
            Directory.CreateDirectory(sourceDir);
            Directory.CreateDirectory(destDir);
            Directory.CreateDirectory(@"C:\FileUtilityTest\Config");

            // Crear archivos REALES de diferentes tipos y fechas
            var testFiles = new[]
            {
            new { Name = "documento_hoy.docx", Content = "Documento Word de hoy", DaysOld = 0 },
            new { Name = "reporte_hoy.pdf", Content = "Reporte PDF de hoy", DaysOld = 0 },
            new { Name = "datos_hoy.xlsx", Content = "Excel de hoy", DaysOld = 0 },
            new { Name = "texto_hoy.txt", Content = "Archivo de texto de hoy", DaysOld = 0 },
            new { Name = "viejo_documento.docx", Content = "Documento viejo", DaysOld = 3 },
            new { Name = "archivo_ayer.txt", Content = "Archivo de ayer", DaysOld = 1 }
        };

            foreach (var file in testFiles)
            {
                var filePath = Path.Combine(sourceDir, file.Name);
                await File.WriteAllTextAsync(filePath, $"{file.Content}\nCreado: {DateTime.Now}\nTamaño: {file.Content.Length} caracteres");

                // Modificar fecha para simular archivos de diferentes días
                var targetDate = DateTime.Now.AddDays(-file.DaysOld);
                File.SetLastWriteTime(filePath, targetDate);
                File.SetCreationTime(filePath, targetDate);

                Console.WriteLine($"   ✅ {file.Name} (modificado: {targetDate:yyyy-MM-dd})");
            }

            // Mostrar resumen
            var allFiles = Directory.GetFiles(sourceDir);
            var todayFiles = allFiles.Where(f => File.GetLastWriteTime(f).Date == DateTime.Today).Count();

            Console.WriteLine($"\n📊 Entorno creado:");
            Console.WriteLine($"   📂 Origen: {sourceDir}");
            Console.WriteLine($"   📂 Destino: {destDir}");
            Console.WriteLine($"   📄 Total archivos: {allFiles.Length}");
            Console.WriteLine($"   📅 Modificados hoy: {todayFiles}");
        }

        private static async Task CreateWorkingTasks(FileUtilityService2 fileUtility)
        {
            Console.WriteLine("\n🔨 Creando tareas FUNCIONALES...");

            // TAREA 1: Backup simple SIN condiciones restrictivas
            var task1 = new FileCopyTask
            {
                Name = "Backup Todos los Archivos",
                SourcePath = @"C:\FileUtilityTest\Source"
            }
            .AddDestination(@"C:\FileUtilityTest\Destination\AllFiles")
            .AddFilePatterns("*.docx", "*.pdf", "*.xlsx", "*.txt")
            // SIN .ModifiedToday() para que copie TODOS los archivos
            .Enable();

            // Programar para cada 2 minutos (para prueba rápida)
            var schedule1 = new ScheduleConfiguration
            {
                Type = ScheduleType.Interval,
                IntervalMinutes = 1,  // Cada 2 minutos
                IsEnabled = true
            };
            schedule1.ExecutionTimes.Add(TimeSpan.Zero); // Requerido para intervalos

            var task1Id = await fileUtility.CreateTaskAsync(task1, schedule1);
            Console.WriteLine($"✅ Tarea 1 creada: {task1.Name} (cada 2 min)");

            /*// TAREA 2: Solo archivos modificados hoy
            var task2 = new FileCopyTask
            {
                Name = "Solo Archivos de Hoy",
                SourcePath = @"C:\FileUtilityTest\Source"
            }
            .AddDestination(@"C:\FileUtilityTest\Destination\TodayOnly")
            .AddFilePattern("*.txt")
            .ModifiedToday()  // Solo archivos de hoy
            .Enable();

            // Programar para cada 3 minutos
            var schedule2 = new ScheduleConfiguration
            {
                Type = ScheduleType.Interval,
                IntervalMinutes = 3,  // Cada 3 minutos
                IsEnabled = true
            };
            schedule2.ExecutionTimes.Add(TimeSpan.Zero);

            var task2Id = await fileUtility.CreateTaskAsync(task2, schedule2);
            Console.WriteLine($"✅ Tarea 2 creada: {task2.Name} (cada 3 min)");*/

            // Crear directorios de destino
            Directory.CreateDirectory(@"C:\FileUtilityTest\Destination\AllFiles");
            Directory.CreateDirectory(@"C:\FileUtilityTest\Destination\TodayOnly");
        }

        private static async Task TestManualExecution(FileUtilityService2 fileUtility)
        {
            Console.WriteLine("\n🧪 PRUEBA MANUAL:");

            var tasks = fileUtility.GetAllTasks();
            if (tasks.Any())
            {
                var firstTask = tasks.First();
                Console.WriteLine($"🔧 Ejecutando: {firstTask.Name}");

                // Mostrar archivos en origen ANTES
                var sourceFiles = Directory.GetFiles(firstTask.SourcePath);
                Console.WriteLine($"📄 Archivos en origen: {sourceFiles.Length}");

                var result = await fileUtility.ExecuteTaskNowAsync(firstTask.Id);

                Console.WriteLine($"📊 Resultado: {result.Status}");
                Console.WriteLine($"   Total: {result.TotalFiles}");
                Console.WriteLine($"   Exitosos: {result.SuccessfulFiles}");
                Console.WriteLine($"   Fallidos: {result.FailedFiles}");

                // Verificar archivos copiados
                var destPath = firstTask.DestinationPaths.FirstOrDefault();
                if (!string.IsNullOrEmpty(destPath) && Directory.Exists(destPath))
                {
                    var copiedFiles = Directory.GetFiles(destPath);
                    Console.WriteLine($"✅ Archivos copiados en destino: {copiedFiles.Length}");
                    foreach (var file in copiedFiles)
                    {
                        Console.WriteLine($"   - {Path.GetFileName(file)}");
                    }
                }
            }
        }

        private static async Task ShowDetailedStatus(FileUtilityService2 fileUtility)
        {
            Console.WriteLine("\n=== ESTADO DETALLADO ===");

            var tasks = fileUtility.GetAllTasks();
            var schedules = fileUtility.GetAllSchedules();

            Console.WriteLine($"📋 Total tareas: {tasks.Count}");
            Console.WriteLine($"⏰ Schedules: {schedules.Count}");
            Console.WriteLine($"🟢 Scheduler activo: {fileUtility.IsSchedulerRunning}");

            Console.WriteLine("\n📋 TAREAS:");
            foreach (var task in tasks)
            {
                Console.WriteLine($"  📁 {task.Name}");
                Console.WriteLine($"     ID: {task.Id}");
                Console.WriteLine($"     Origen: {task.SourcePath}");
                Console.WriteLine($"     Destinos: {string.Join(", ", task.DestinationPaths)}");
                Console.WriteLine($"     Patrones: {string.Join(", ", task.FilePatterns)}");
                Console.WriteLine($"     Condiciones: {task.Conditions.Count}");
                Console.WriteLine($"     Habilitada: {task.IsEnabled}");

                // Mostrar próximas ejecuciones usando el scheduler interno
                var nextExecutions = await fileUtility.GetNextExecutionTimesAsync(task.Id, 3);
                if (nextExecutions.Any() && nextExecutions.First() != DateTime.MinValue)
                {
                    Console.WriteLine($"     Próximas ejecuciones:");
                    foreach (var next in nextExecutions.Take(3))
                    {
                        if (next > DateTime.Now)
                        {
                            var timeUntil = next - DateTime.Now;
                            Console.WriteLine($"       - {next:dd/MM/yyyy HH:mm:ss} (en {timeUntil.TotalMinutes:F0} min)");
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"     ⚠️ Sin próximas ejecuciones programadas");
                }
            }

            Console.WriteLine("\n⏰ SCHEDULES:");
            foreach (var schedule in schedules)
            {
                Console.WriteLine($"  📅 Task: {schedule.TaskId}");
                Console.WriteLine($"     Tipo: {schedule.Type}");
                Console.WriteLine($"     Intervalo: {schedule.IntervalMinutes} min");
                Console.WriteLine($"     Habilitado: {schedule.IsEnabled}");
            }
        }

        private static void ConfigureEvents(FileUtilityService2 fileUtility)
        {
            fileUtility.OperationStarted += (sender, e) =>
            {
                Console.WriteLine($"\n🚀 [INICIANDO] {e.Result.TaskName} - {e.Result.StartTime:HH:mm:ss}");
            };

            fileUtility.OperationCompleted += (sender, e) =>
            {
                var statusIcon = e.Result.Status switch
                {
                    CopyStatus.Completed => "✅",
                    CopyStatus.PartialSuccess => "⚠️",
                    CopyStatus.Failed => "❌",
                    _ => "ℹ️"
                };

                Console.WriteLine($"{statusIcon} [COMPLETADO] {e.Result.TaskName}");
                Console.WriteLine($"   Estado: {e.Result.Status}");
                Console.WriteLine($"   Archivos: {e.Result.SuccessfulFiles}/{e.Result.TotalFiles} exitosos");
                Console.WriteLine($"   Duración: {e.Result.Duration.TotalSeconds:F1}s");

                if (!string.IsNullOrEmpty(e.Result.GeneralError))
                    Console.WriteLine($"   ❌ Error: {e.Result.GeneralError}");
            };

            fileUtility.FileProcessed += (sender, e) =>
            {
                var status = e.Result.Success ? "✓" : "✗";
                var fileName = Path.GetFileName(e.Result.FilePath);
                Console.WriteLine($"  {status} {fileName} -> {Path.GetFileName(e.Result.DestinationPath)}");

                if (!e.Result.Success && !string.IsNullOrEmpty(e.Result.ErrorMessage))
                    Console.WriteLine($"    Error: {e.Result.ErrorMessage}");
            };

            fileUtility.TaskExecuting += (sender, e) =>
            {
                Console.WriteLine($"\n⏰ [PROGRAMADA] Ejecutando {e.TaskName} (programada: {e.ScheduledTime:HH:mm:ss})");
            };
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
                        await ShowDetailedStatus(fileUtility);
                        break;

                    case 'e':
                    case 'E':
                        await TestManualExecution(fileUtility);
                        break;

                    case 'c':
                    case 'C':
                        await ShowCopiedFiles();
                        break;

                    case 'f':
                    case 'F':
                        await ShowSourceFiles();
                        break;

                    default:
                        Console.WriteLine("\n❓ Comandos disponibles:");
                        Console.WriteLine("   's' - Ver estado del sistema");
                        Console.WriteLine("   'e' - Ejecutar tarea manualmente");
                        Console.WriteLine("   'c' - Ver archivos copiados");
                        Console.WriteLine("   'f' - Ver archivos fuente");
                        Console.WriteLine("   'q' - Salir");
                        break;
                }
            }
        }

        private static async Task ShowCopiedFiles()
        {
            Console.WriteLine("\n📁 ARCHIVOS COPIADOS:");

            var destDirs = new[]
            {
            @"C:\FileUtilityTest\Destination\AllFiles",
            @"C:\FileUtilityTest\Destination\TodayOnly"
        };

            foreach (var dir in destDirs)
            {
                if (Directory.Exists(dir))
                {
                    var files = Directory.GetFiles(dir);
                    Console.WriteLine($"\n📂 {Path.GetFileName(dir)}: {files.Length} archivos");
                    foreach (var file in files)
                    {
                        var fileInfo = new FileInfo(file);
                        Console.WriteLine($"   - {Path.GetFileName(file)} ({fileInfo.Length} bytes, {fileInfo.LastWriteTime:HH:mm:ss})");
                    }
                }
                else
                {
                    Console.WriteLine($"\n📂 {Path.GetFileName(dir)}: directorio no existe");
                }
            }
        }

        private static async Task ShowSourceFiles()
        {
            Console.WriteLine("\n📁 ARCHIVOS FUENTE:");

            var sourceDir = @"C:\FileUtilityTest\Source";
            if (Directory.Exists(sourceDir))
            {
                var files = Directory.GetFiles(sourceDir);
                Console.WriteLine($"📂 {files.Length} archivos en origen:");

                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    var isToday = fileInfo.LastWriteTime.Date == DateTime.Today;
                    var dayIcon = isToday ? "📅" : "📄";

                    Console.WriteLine($"   {dayIcon} {Path.GetFileName(file)} (modificado: {fileInfo.LastWriteTime:dd/MM HH:mm})");
                }
            }
        }
    }
}
