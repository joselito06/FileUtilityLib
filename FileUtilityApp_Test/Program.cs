using FileUtilityLib.Core;
using FileUtilityLib.Models;
using FileUtilityLib.Scheduler;

internal class Program
{
    private static void Main(string[] args)
    {
        // Crear una solicitud de copiado
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
        var scheduledTask = new ScheduledTask
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

        var scheduler = new TaskSchedulerService();
        scheduler.AddTask(scheduledTask);

        Console.WriteLine("✅ Tarea programada iniciada. Presiona una tecla para detener...");
        Console.ReadKey();

        scheduler.StopAll();
        Console.WriteLine("⏹️ Tareas detenidas.");
    }
}