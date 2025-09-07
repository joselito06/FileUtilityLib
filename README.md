# FileUtilityLib

Una librería completa para .NET que permite la copia automatizada de archivos con condiciones personalizables y programación de tareas. Soporta tanto .NET 8.0 como .NET Framework 4.7.2+.

## 🚀 Características

- **Copia Condicional de Archivos**: Copia archivos basado en condiciones como fecha de modificación, tamaño, extensión, etc.
- **Múltiples Destinos**: Copia archivos a uno o múltiples destinos simultáneamente
- **Programación Avanzada**: Programa tareas para ejecutarse diariamente, semanalmente, mensualmente o por intervalos
- **Filtrado Flexible**: Incluye/excluye días específicos de la semana (ej. solo días laborales)
- **Eventos en Tiempo Real**: Monitorea el progreso de las operaciones en tiempo real
- **Persistencia**: Guarda y carga configuraciones automáticamente
- **Multi-Target**: Compatible con .NET 8.0 y .NET Framework 4.7.2+
- **Thread-Safe**: Diseñado para uso concurrente seguro

## 📦 Instalación

```xml
<PackageReference Include="FileUtilityLib" Version="1.0.0" />
```

O clona el repositorio y compila localmente:

```bash
git clone [repository-url]
cd FileUtilityLib
dotnet build
```

## 🛠️ Uso Básico

### Configuración Inicial

```csharp
using FileUtilityLib.Extensions;

// Crear el servicio principal
using var fileUtility = ServiceCollectionExtensions.CreateFileUtilityService(@"C:\MyConfig");

// Iniciar el programador de tareas
await fileUtility.StartSchedulerAsync();
```

### Ejemplo 1: Backup Diario Simple

```csharp
using FileUtilityLib.Models;
using FileUtilityLib.Extensions;

// Crear tarea para copiar documentos modificados hoy
var task = new FileCopyTask
{
    Name = "Backup Documentos Diario",
    SourcePath = @"C:\Users\Documents"
}
.AddDestination(@"D:\Backup\Documents")
.AddFilePatterns("*.docx", "*.pdf", "*.xlsx")
.ModifiedToday()  // Solo archivos modificados hoy
.Enable();

// Programar para ejecutarse a las 8:00 AM y 6:00 PM, solo días laborales
var schedule = new ScheduleConfiguration()
    .Daily()
    .AddExecutionTime(8, 0)   // 8:00 AM
    .AddExecutionTime(18, 0)  // 6:00 PM
    .OnWeekdays()            // Solo lunes a viernes
    .Enable();

// Crear la tarea programada
var taskId = await fileUtility.CreateTaskAsync(task, schedule);
```

### Ejemplo 2: Filtrado Avanzado

```csharp
// Tarea con múltiples condiciones y destinos
var complexTask = new FileCopyTask
{
    Name = "Archivos Grandes Recientes",
    SourcePath = @"C:\Data"
}
.AddDestinations(@"\\Server1\Backup", @"\\Server2\Mirror")
.AddFilePattern("*.log")
.ModifiedSince(DateTime.Today.AddDays(-7))     // Última semana
.FileSizeGreaterThan(10 * 1024 * 1024)        // Mayores a 10MB
.WithFileExtension("log")                       // Solo archivos .log
.Enable();

// Programar para lunes, miércoles y viernes a las 2:00 AM
var weeklySchedule = new ScheduleConfiguration()
    .Weekly()
    .OnDays(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday)
    .AddExecutionTime(2, 0)
    .Enable();

await fileUtility.CreateTaskAsync(complexTask, weeklySchedule);
```

### Ejemplo 3: Monitoreo por Intervalos

```csharp
// Tarea que se ejecuta cada 30 minutos
var monitorTask = new FileCopyTask
{
    Name = "Monitoreo Logs",
    SourcePath = @"C:\Logs"
}
.AddDestination(@"C:\Backup\Logs")
.ModifiedSince(DateTime.Now.AddHours(-1))  // Última hora
.Enable();

var intervalSchedule = new ScheduleConfiguration()
    .EveryMinutes(30)  // Cada 30 minutos
    .StartingAt(DateTime.Now)
    .Enable();

await fileUtility.CreateTaskAsync(monitorTask, intervalSchedule);
```

## 📋 Tipos de Condiciones

| Condición | Descripción | Ejemplo |
|-----------|-------------|---------|
| `ModifiedToday()` | Archivos modificados hoy | `.ModifiedToday()` |
| `ModifiedSince(fecha)` | Archivos modificados desde una fecha | `.ModifiedSince(DateTime.Today.AddDays(-7))` |
| `CreatedToday()` | Archivos creados hoy | `.CreatedToday()` |
| `CreatedSince(fecha)` | Archivos creados desde una fecha | `.CreatedSince(DateTime.Today.AddMonths(-1))` |
| `FileSizeGreaterThan(bytes)` | Archivos mayores a un tamaño | `.FileSizeGreaterThan(1024 * 1024)` |
| `FileSizeLessThan(bytes)` | Archivos menores a un tamaño | `.FileSizeLessThan(500 * 1024)` |
| `WithFileExtension(ext)` | Archivos con extensión específica | `.WithFileExtension("pdf")` |
| `ContainingFileName(pattern)` | Archivos que contengan un patrón | `.ContainingFileName("report")` |

## ⏰ Tipos de Programación

### Programación Diaria
```csharp
var schedule = new ScheduleConfiguration()
    .Daily()
    .AddExecutionTime(9, 0)    // 9:00 AM
    .AddExecutionTime(21, 0)   // 9:00 PM
    .Enable();
```

### Programación Semanal
```csharp
var schedule = new ScheduleConfiguration()
    .Weekly()
    .OnWeekdays()              // Lunes a Viernes
    .AddExecutionTime(8, 30)   // 8:30 AM
    .Enable();

// O días específicos
var customSchedule = new ScheduleConfiguration()
    .Weekly()
    .OnDays(DayOfWeek.Tuesday, DayOfWeek.Thursday)
    .AddExecutionTime(14, 0)   // 2:00 PM
    .Enable();
```

### Programación Mensual
```csharp
var schedule = new ScheduleConfiguration()
    .Monthly()
    .AddExecutionTime(1, 0)    // 1:00 AM del primer día del mes
    .Enable();
```

### Programación por Intervalos
```csharp
var schedule = new ScheduleConfiguration()
    .EveryMinutes(15)          // Cada 15 minutos
    .Between(DateTime.Today.AddDays(1), DateTime.Today.AddMonths(1))
    .Enable();
```

## 📡 Eventos y Monitoreo

```csharp
// Suscribirse a eventos del sistema
fileUtility.OperationStarted += (sender, e) =>
{
    Console.WriteLine($"Iniciando: {e.Result.TaskName}");
};

fileUtility.OperationCompleted += (sender, e) =>
{
    Console.WriteLine($"Completado: {e.Result.TaskName}");
    Console.WriteLine($"Archivos procesados: {e.Result.TotalFiles}");
    Console.WriteLine($"Exitosos: {e.Result.SuccessfulFiles}");
    Console.WriteLine($"Duración: {e.Result.Duration}");
};

fileUtility.FileProcessed += (sender, e) =>
{
    var status = e.Result.Success ? "✓" : "✗";
    Console.WriteLine($"{status} {Path.GetFileName(e.Result.FilePath)}");
};

fileUtility.TaskExecuting += (sender, e) =>
{
    Console.WriteLine($"Ejecutando tarea programada: {e.TaskName}");
};
```

## 🔧 Gestión de Tareas

### Ejecución Manual
```csharp
// Ejecutar una tarea inmediatamente
var result = await fileUtility.ExecuteTaskNowAsync(taskId);

if (result.Status == CopyStatus.Completed)
{
    Console.WriteLine($"Tarea completada exitosamente en {result.Duration}");
}
```

### Consultar Estado
```csharp
// Obtener todas las tareas
var tasks = fileUtility.GetAllTasks();

// Obtener próximas ejecuciones
var nextExecutions = await fileUtility.GetNextExecutionTimesAsync(taskId, 5);

foreach (var next in nextExecutions)
{
    Console.WriteLine($"Próxima ejecución: {next:yyyy-MM-dd HH:mm:ss}");
}
```

### Actualizar Tareas
```csharp
// Obtener tarea existente
var task = fileUtility.GetAllTasks().First();

// Modificar condiciones
task.AddCondition(ConditionType.FileSizeGreaterThan, 5 * 1024 * 1024);

// Actualizar
await fileUtility.UpdateTaskAsync(task);
```

### Eliminar Tareas
```csharp
// Eliminar tarea (también cancela su programación)
await fileUtility.DeleteTaskAsync(taskId);
```

## 📁 Estructura del Proyecto

```
FileUtilityLib/
├── Core/
│   ├── Interfaces/          # Interfaces principales
│   └── Services/           # Implementaciones de servicios
├── Models/                 # Modelos de datos y eventos
├── Scheduler/              # Servicios de programación
├── Extensions/             # Métodos de extensión
└── Example/               # Ejemplos de uso
```

## 🎯 Casos de Uso Comunes

### 1. Backup Automático de Documentos
```csharp
var backupTask = new FileCopyTask { Name = "Backup Documentos", SourcePath = @"C:\Documents" }
    .AddDestination(@"D:\Backup")
    .ModifiedToday()
    .AddFilePatterns("*.docx", "*.xlsx", "*.pdf");

var dailySchedule = new ScheduleConfiguration()
    .Daily().AddExecutionTime(20, 0).OnWeekdays();
```

### 2. Sincronización de Logs
```csharp
var logSync = new FileCopyTask { Name = "Sync Logs", SourcePath = @"C:\App\Logs" }
    .AddDestinations(@"\\Server1\Logs", @"\\Server2\Logs")
    .ModifiedSince(DateTime.Now.AddHours(-2))
    .WithFileExtension("log");

var intervalSchedule = new ScheduleConfiguration().EveryMinutes(30);
```

### 3. Archivado Mensual
```csharp
var archiveTask = new FileCopyTask { Name = "Archivo Mensual", SourcePath = @"C:\Data" }
    .AddDestination(@"\\Archive\Monthly")
    .ModifiedSince(DateTime.Today.AddDays(-30))
    .FileSizeGreaterThan(1024 * 1024);

var monthlySchedule = new ScheduleConfiguration()
    .Monthly().AddExecutionTime(2, 0);
```

## ⚙️ Configuración Avanzada

### Directorio de Configuración Personalizado
```csharp
// Especificar directorio personalizado para configuración
using var service = ServiceCollectionExtensions.CreateFileUtilityService(@"C:\MyApp\Config");
```

### Inyección de Dependencias
```csharp
services.AddFileUtilityLib(@"C:\MyApp\Config");

// En tu controlador o servicio
public class MyService
{
    private readonly IFileUtilityService _fileUtility;
    
    public MyService(IFileUtilityService fileUtility)
    {
        _fileUtility = fileUtility;
    }
}
```

## 🐛 Solución de Problemas

### La tarea no se ejecuta
- Verifica que el programador esté iniciado: `fileUtility.IsSchedulerRunning`
- Confirma que la tarea esté habilitada: `task.IsEnabled = true`
- Revisa los logs para errores específicos

### Archivos no se copian
- Verifica permisos en rutas origen y destino
- Confirma que las condiciones sean correctas
- Usa `GetFilesToCopy()` para ver qué archivos coinciden

### Problemas de rendimiento
- Para archivos grandes, considera usar menos destinos simultáneos
- Ajusta el buffer de copia si es necesario
- Monitorea el uso de memoria y disco

## 📝 Logging

La librería utiliza `Microsoft.Extensions.Logging`. Para habilitar logging detallado:

```csharp
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Debug);
});
```

## 🔒 Consideraciones de Seguridad

- Asegúrate de que las cuentas de servicio tengan permisos apropiados
- Valida todas las rutas de entrada para prevenir ataques de path traversal
- Considera cifrar archivos de configuración si contienen rutas sensibles

## 🤝 Contribuciones

Las contribuciones son bienvenidas. Por favor:

1. Fork el proyecto
2. Crea una rama para tu feature (`git checkout -b feature/AmazingFeature`)
3. Commit tus cambios (`git commit -m 'Add AmazingFeature'`)
4. Push a la rama (`git push origin feature/AmazingFeature`)
5. Abre un Pull Request

## 📄 Licencia

Este proyecto está licenciado bajo la Licencia MIT - ver el archivo [LICENSE](LICENSE) para detalles.

## 📞 Soporte

Para soporte o preguntas:
- Abre un issue en GitHub
- Revisa la documentación de ejemplos
- Consulta los logs para información detallada de errorestilityLib
