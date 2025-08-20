using FileSchedulerLib.Conditions;
using FileSchedulerLib.Core;
using FileUtilityLib.Core;
using FileUtilityLib.Models;

internal class Program
{
    static void Main(string[] args)
    {
        /*// Crear una solicitud de copiado
        var copyRequest = new FileCopyRequest
        {
            SourceFolder = @"C:\Users\Joselito\Desktop\Ruta A",
            FilesToCopy = new List<string> { "Prueba 1.xlsx", "Prueba 2.xlsx" },
            DestinationFolders = new List<string> { @"C:\Users\Joselito\Documents\Ruta B" },
            Overwrite = true
        };

        // Definir la tarea de copiado
        var copyTask = new CopyFilesTask(copyRequest);

        // Programar la tarea
        var scheduledTask = new FileUtilityLib.Scheduler.ScheduledTask
        {
            Name = "CopiaDiaria",
            TaskAction = copyTask,
            ExecutionTimes = new List<TimeSpan>
            {
                new TimeSpan(21,27,0),   // 8:00 AM
                new TimeSpan(21,29,0),  // 9:30 AM
                new TimeSpan(21,30,0)   // 12:00 PM
            },
            ExcludedDays = new List<DayOfWeek> { DayOfWeek.Saturday, DayOfWeek.Sunday }
        };

        var scheduler = new FileUtilityLib.Scheduler.TaskSchedulerService();
        scheduler.AddTask(scheduledTask);

        Console.WriteLine("✅ Tarea programada iniciada. Presiona una tecla para detener...");
        Console.ReadKey();

        scheduler.StopAll();
        Console.WriteLine("⏹️ Tareas detenidas.");*/


        var scheduler2 = new TaskSchedulerService();

        var task = new ScheduledTask
        {
            Name = "CopiaDiaria",
            TaskAction = async () =>
            {
                Console.WriteLine($"Ejecutando tarea: {DateTime.Now}");
                await Task.Delay(500); // Simula la tarea
            },
            RunTimes = new List<TimeSpan>
            {
                new TimeSpan(8,0,0),
                new TimeSpan(12,0,0),
                new TimeSpan(18,30,0)
            },
            ExcludedDays = new List<DayOfWeek> { DayOfWeek.Saturday, DayOfWeek.Sunday },
            Conditions = new List<ITaskCondition>
            {
                new CustomCondition( () => DateTime.Now.Day % 2 == 0) // solo días pares
            }
        };

        // Suscribirse a eventos (simulación con consola)
        task.TaskAction = WrapWithEvents(task, task.TaskAction);

        scheduler2.AddTask(task);

        Console.WriteLine("Scheduler iniciado. Presiona ENTER para detener...");
        Console.ReadLine();

        scheduler2.StopAll();
        Console.WriteLine("Scheduler detenido.");
    }

    // Función que envuelve la acción con notificación de eventos
    static Func<Task> WrapWithEvents(ScheduledTask task, Func<Task> action)
    {
        return async () =>
        {
            OnTaskStarted(task.Name);
            try
            {
                await action();
                OnTaskCompleted(task.Name);
            }
            catch (Exception ex)
            {
                OnTaskFailed(task.Name, ex);
            }
        };
    }

    // Eventos simulados (puedes reemplazar por EventHandler reales)
    static void OnTaskStarted(string name) =>
        Console.WriteLine($"▶️ Tarea iniciada: {name} ({DateTime.Now})");

    static void OnTaskCompleted(string name) =>
        Console.WriteLine($"✅ Tarea completada: {name} ({DateTime.Now})");

    static void OnTaskFailed(string name, Exception ex) =>
        Console.WriteLine($"❌ Tarea fallida: {name} ({DateTime.Now}) → {ex.Message}");
}