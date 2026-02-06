# FileUtilityLib

Una librería completa para .NET que permite la copia automatizada de archivos con condiciones personalizables, selección específica de archivos, manejo inteligente de duplicados, organización por fecha y programación de tareas. Soporta tanto .NET 8.0 como .NET Framework 4.7.2+.

## 🚀 Características

- **Copia Condicional de Archivos**: Copia archivos basado en condiciones como fecha de modificación, tamaño, extensión, etc.
- **🎯 Selección de Archivos Específicos**: Especifica archivos exactos por nombre (ej: "Reporte1.xlsx", "Config.json")
- **🛡️ Manejo Inteligente de Duplicados**: Control total sobre qué hacer cuando un archivo ya existe
- **🔍 Múltiples Algoritmos de Comparación**: Desde comparación rápida hasta verificación precisa por contenido
- **📁 Organización por Fecha**: Organiza automáticamente archivos en carpetas por fecha
- **Múltiples Destinos**: Copia archivos a uno o múltiples destinos simultáneamente
- **Programación Avanzada**: Programa tareas para ejecutarse diariamente, semanalmente, mensualmente o por intervalos
- **Filtrado Flexible**: Incluye/excluye días específicos de la semana (ej. solo días laborales)
- **Eventos en Tiempo Real**: Monitorea el progreso de las operaciones en tiempo real
- **Persistencia**: Guarda y carga configuraciones automáticamente
- **Multi-Target**: Compatible con .NET 8.0 y .NET Framework 4.7.2+
- **Thread-Safe**: Diseñado para uso concurrente seguro

## 📦 Instalación

```xml
<PackageReference Include="FileUtilityLib" Version="1.2.0" />
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

## 🎯 Nuevas Funcionalidades v1.2.0

### **📁 Organización Automática por Fecha**

Organiza archivos copiados en subcarpetas basadas en la fecha de ejecución. Ideal para backups organizados y archivos históricos.

#### **Características:**
- ✅ Creación automática de carpetas por fecha
- ✅ Formatos de fecha completamente personalizables
- ✅ Compatible con todas las demás funcionalidades
- ✅ Organización jerárquica (Año/Mes/Día si lo deseas)

#### **Uso Básico:**

```csharp
var task = new FileCopyTask
{
    Name = "Backup Organizado por Fecha",
    SourcePath = @"C:\Documents"
}
.AddDestination(@"D:\Backup")
.AddFilePattern("*.pdf")
.OrganizeByDateFolder()  // ✅ Usar formato por defecto: dd-MM-yyyy
.Enable();

// Resultado si se ejecuta el 05/02/2026:
// D:\Backup\05-02-2026\archivo1.pdf
// D:\Backup\05-02-2026\archivo2.pdf
```

#### **Formatos de Fecha Disponibles:**

| Formato | Ejemplo | Resultado | Uso Recomendado |
|---------|---------|-----------|-----------------|
| `dd-MM-yyyy` | 05-02-2026 | `Backup\05-02-2026\` | Backup diario (predeterminado) |
| `yyyy-MM-dd` | 2026-02-05 | `Backup\2026-02-05\` | Ordenamiento ISO estándar |
| `yyyy-MM` | 2026-02 | `Backup\2026-02\` | Archivo mensual |
| `yyyy` | 2026 | `Backup\2026\` | Archivo anual |
| `yyyy-MM-dd_HH-mm` | 2026-02-05_14-30 | `Backup\2026-02-05_14-30\` | Backups con timestamp |
| `yyyyMMdd` | 20260205 | `Backup\20260205\` | Sin separadores |
| `yyyy\\MM\\dd` | 2026\02\05 | `Backup\2026\02\05\` | Estructura jerárquica |

#### **Ejemplos de Uso:**

```csharp
// Ejemplo 1: Backup diario con formato ISO
var dailyBackup = new FileCopyTask
{
    Name = "Backup Diario ISO",
    SourcePath = @"C:\Projects"
}
.AddDestination(@"D:\DailyBackup")
.AddFilePattern("*.cs")
.OrganizeByDateFolder("yyyy-MM-dd")
.ModifiedToday()
.Enable();

// Resultado: D:\DailyBackup\2026-02-05\Program.cs

// Ejemplo 2: Archivo mensual
var monthlyArchive = new FileCopyTask
{
    Name = "Archivo Mensual",
    SourcePath = @"C:\Reports"
}
.AddDestination(@"D:\MonthlyArchive")
.AddFilePattern("*.xlsx")
.OrganizeByDateFolder("yyyy-MM")
.Enable();

// Resultado: D:\MonthlyArchive\2026-02\report.xlsx

// Ejemplo 3: Backups con timestamp completo
var timestampBackup = new FileCopyTask
{
    Name = "Backup con Hora",
    SourcePath = @"C:\Database"
}
.AddDestination(@"D:\DatabaseBackup")
.AddFilePattern("*.bak")
.OrganizeByDateFolder("yyyy-MM-dd_HH-mm")
.Enable();

// Resultado: D:\DatabaseBackup\2026-02-05_14-30\database.bak

// Ejemplo 4: Estructura jerárquica (Año/Mes/Día)
var hierarchicalBackup = new FileCopyTask
{
    Name = "Backup Jerárquico",
    SourcePath = @"C:\Photos"
}
.AddDestination(@"D:\PhotoArchive")
.AddFilePattern("*.jpg")
.OrganizeByDateFolder(@"yyyy\\MM\\dd")  // Usar \\ para separadores
.Enable();

// Resultado: D:\PhotoArchive\2026\02\05\photo.jpg
```

### **🔄 Combinación con Otras Funcionalidades**

La organización por fecha se combina perfectamente con todas las características existentes:

```csharp
// Ejemplo avanzado: Fecha + Archivos específicos + Duplicados
var advancedTask = new FileCopyTask
{
    Name = "Backup Avanzado Organizado",
    SourcePath = @"C:\ImportantDocs"
}
.AddDestinations(@"D:\LocalBackup", @"\\Server\NetworkBackup")
.AddSpecificFiles("Reporte_Final.xlsx", "Contrato.pdf", "Config.json")
.OrganizeByDateFolder("yyyy-MM-dd")     // ✅ Carpetas por fecha
.OverwriteIfNewer()                      // ✅ Solo si es más nuevo
.CompareBySizeAndDate()                  // ✅ Comparación rápida
.Enable();

var schedule = new ScheduleConfiguration()
    .Daily()
    .AddExecutionTime(20, 0)  // 8 PM diario
    .OnWeekdays()
    .Enable();

await fileUtility.CreateTaskAsync(advancedTask, schedule);

// Resultado cada día laboral a las 8 PM:
// D:\LocalBackup\2026-02-05\Reporte_Final.xlsx
// D:\LocalBackup\2026-02-05\Contrato.pdf
// D:\LocalBackup\2026-02-06\Reporte_Final.xlsx  (siguiente día)
```

### **💡 Casos de Uso Prácticos**

#### **1. Backup Incremental Diario**
```csharp
var incrementalBackup = new FileCopyTask
{
    Name = "Backup Incremental",
    SourcePath = @"C:\WorkDocs"
}
.AddDestination(@"D:\IncrementalBackup")
.AddFilePatterns("*.docx", "*.xlsx", "*.pptx")
.OrganizeByDateFolder("yyyy-MM-dd")
.ModifiedToday()
.SkipDuplicates()
.Enable();

// Cada día crea una nueva carpeta solo con archivos modificados ese día
```

#### **2. Logs Organizados por Mes**
```csharp
var logArchive = new FileCopyTask
{
    Name = "Archivo de Logs Mensual",
    SourcePath = @"C:\AppLogs"
}
.AddDestination(@"D:\LogArchive")
.AddFilePattern("*.log")
.OrganizeByDateFolder("yyyy-MM")
.Enable();

// Agrupa todos los logs del mes en una sola carpeta
```

#### **3. Fotos Organizadas Jerárquicamente**
```csharp
var photoOrganizer = new FileCopyTask
{
    Name = "Organizar Fotos",
    SourcePath = @"C:\Camera\Import"
}
.AddDestination(@"D:\Photos")
.AddFilePatterns("*.jpg", "*.png", "*.raw")
.OrganizeByDateFolder(@"yyyy\\MM\\dd")  // Año\Mes\Día
.CreatedToday()
.RenameIfExists()  // No sobrescribir fotos existentes
.Enable();

// Organiza fotos en estructura: D:\Photos\2026\02\05\
```

#### **4. Backups Cada 4 Horas con Timestamp**
```csharp
var frequentBackup = new FileCopyTask
{
    Name = "Backup Frecuente",
    SourcePath = @"C:\ActiveProject"
}
.AddDestination(@"D:\HourlyBackup")
.AddFilePattern("*.*")
.OrganizeByDateFolder("yyyy-MM-dd_HH-mm")
.ModifiedSince(DateTime.Now.AddHours(-4))
.Enable();

var schedule = new ScheduleConfiguration()
    .EveryMinutes(240)  // Cada 4 horas
    .Enable();

// Crea carpetas: 2026-02-05_08-00, 2026-02-05_12-00, etc.
```

### **⚙️ Deshabilitar Organización por Fecha**

```csharp
var task = new FileCopyTask { Name = "Sin Organizar" }
    .AddDestination(@"D:\Backup")
    .DisableDateOrganization()  // Explícitamente deshabilitar
    .Enable();

// O simplemente no llamar .OrganizeByDateFolder()
```

## 🎯 Funcionalidades v1.1.0

### **📂 Selección de Archivos Específicos**

Especifica archivos exactos por nombre, sin usar patrones:

```csharp
var task = new FileCopyTask
{
    Name = "Solo Archivos Específicos",
    SourcePath = @"C:\Documents"
}
.AddDestination(@"D:\Backup")
.AddSpecificFiles("Reporte_Final.xlsx", "Ventas_Q1.pdf", "Config.json")
.Enable();
```

### **🛡️ Manejo Inteligente de Duplicados**

Control total sobre qué hacer cuando un archivo ya existe:

```csharp
// Estrategias disponibles:
.SkipDuplicates()         // Saltar si existe igual
.OverwriteAlways()        // Sobrescribir siempre
.OverwriteIfNewer()       // Solo si es más nuevo
.RenameIfExists()         // Renombrar archivo nuevo

// Algoritmos de comparación:
.CompareBySizeAndDate()   // Rápido (predeterminado)
.CompareBySizeOnly()      // Solo tamaño
.CompareByDateOnly()      // Solo fecha
.CompareByContent()       // Hash SHA-256 (preciso pero lento)
```

## 📋 Ejemplos Completos

### Ejemplo 1: Backup Inteligente con Fecha

```csharp
var task = new FileCopyTask
{
    Name = "Backup Inteligente Documentos",
    SourcePath = @"C:\ImportantDocs"
}
.AddDestination(@"D:\Backup\Docs")
.AddSpecificFiles(
    "Contrato_Principal.pdf",
    "Presupuesto_2024.xlsx", 
    "Configuracion_Sistema.json"
)
.OrganizeByDateFolder("yyyy-MM-dd")  // ✅ Organizar por fecha
.OverwriteIfNewer()                   // Solo si el origen es más reciente
.CompareBySizeAndDate()               // Comparación rápida
.Enable();

var schedule = new ScheduleConfiguration()
    .Daily()
    .AddExecutionTime(18, 0)  // 6 PM
    .OnWeekdays()
    .Enable();

var taskId = await fileUtility.CreateTaskAsync(task, schedule);
```

### Ejemplo 2: Sincronización Segura

```csharp
var syncTask = new FileCopyTask
{
    Name = "Sync Seguro con Fecha",
    SourcePath = @"C:\ProjectFiles"
}
.AddDestinations(@"\\Server1\Projects", @"\\Server2\Backup")
.AddFilePattern("*.docx")
.OrganizeByDateFolder("yyyy\\MM")     // ✅ Carpetas Año\Mes
.ModifiedToday()
.RenameIfExists()                     // No sobrescribir
.CompareByContent()                   // Comparación precisa
.Enable();
```

### Ejemplo 3: Archivo Histórico

```csharp
var archiveTask = new FileCopyTask
{
    Name = "Archivo Histórico",
    SourcePath = @"C:\CompletedProjects"
}
.AddDestination(@"D:\Archive")
.AddFilePatterns("*.zip", "*.rar")
.OrganizeByDateFolder("yyyy")         // ✅ Una carpeta por año
.FileSizeGreaterThan(1024 * 1024)    // Mayores a 1MB
.SkipDuplicates()
.Enable();

var schedule = new ScheduleConfiguration()
    .Monthly()
    .AddExecutionTime(1, 0)  // Primer día del mes
    .Enable();
```

## 📊 Tabla de Comparación de Estrategias

| Estrategia | Velocidad | Precisión | Uso Recomendado |
|------------|-----------|-----------|------------------|
| `SkipDuplicates` + `SizeAndDate` | ⚡ Muy Rápida | ✅ Alta | Backup general, archivos grandes |
| `OverwriteIfNewer` + `DateOnly` | ⚡ Muy Rápida | ✅ Media | Sincronización de documentos |
| `RenameIfExists` + `Content` | 🐌 Lenta | 🎯 Perfecta | Archivos críticos, sin pérdidas |
| `OverwriteAlways` | ⚡ Muy Rápida | ➖ N/A | Reemplazo forzado |

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

// Modificar configuración
task.OrganizeByDate = true;
task.DateFolderFormat = "yyyy-MM-dd";

// Actualizar
await fileUtility.UpdateTaskAsync(task);
```

### Eliminar Tareas
```csharp
// Eliminar tarea (también cancela su programación)
await fileUtility.DeleteTaskAsync(taskId);
```

## 💡 Consejos de Rendimiento

- **Para archivos grandes (>100MB)**: Usa `SizeAndDate` 
- **Para archivos críticos pequeños**: Usa `HashContent`
- **Para sincronización frecuente**: Usa `OverwriteIfNewer`
- **Para archivos únicos**: Usa `RenameIfExists`
- **Para organización histórica**: Usa `.OrganizeByDateFolder()` con formato apropiado
- **Para backups incrementales**: Combina `.OrganizeByDateFolder()` + `.ModifiedToday()`

## 📁 Estructura del Proyecto

```
FileUtilityLib/
├── Core/
│   ├── Interfaces/          # Interfaces principales
│   └── Services/           # Implementaciones de servicios
├── Models/                 # Modelos de datos y eventos
├── Scheduler/              # Servicios de programación personalizada
├── Extensions/             # Métodos de extensión
└── Examples/              # Ejemplos de uso
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
- Si usas archivos específicos, verifica que existan exactamente con esos nombres

### Las carpetas de fecha no se crean
- Verifica que `.OrganizeByDateFolder()` esté llamado
- Confirma permisos de escritura en el directorio destino
- Revisa el formato de fecha (no uses caracteres inválidos como `:` o `?`)
- Usa `\\` en lugar de `\` en el formato para subcarpetas (ej: `yyyy\\MM\\dd`)

### Problemas de duplicados
- Verifica la configuración de `DuplicateHandling`
- Para archivos grandes, usa `CompareBySizeAndDate` en lugar de `CompareByContent`
- Revisa los logs para ver por qué se saltaron archivos

### Problemas de rendimiento
- Para archivos grandes, considera usar menos destinos simultáneos
- Usa `CompareBySizeAndDate` en lugar de `CompareByContent` para mejor rendimiento
- Ajusta el intervalo de programación según la carga del sistema
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
- Al usar `CompareByContent`, ten en cuenta el costo computacional adicional
- Verifica permisos de escritura antes de habilitar organización por fecha

## 🆕 Changelog

### v1.2.0 (Actual)
#### ✅ Nuevas Funcionalidades
- **Organización automática por fecha**: `.OrganizeByDateFolder(format)`
- **Formatos de fecha personalizables**: Soporta cualquier formato válido de DateTime
- **Estructura jerárquica**: Crea carpetas anidadas (Año\Mes\Día)
- **Combinación con duplicados**: Trabaja perfectamente con todas las estrategias existentes

#### 🔧 Mejoras
- **Creación automática de directorios**: Las carpetas de fecha se crean automáticamente
- **Mejor logging**: Información detallada sobre carpetas creadas
- **Manejo de errores robusto**: Fallback a ruta base si falla crear carpeta de fecha

### v1.1.0
#### ✅ Nuevas Funcionalidades
- **Selección de archivos específicos**: `.AddSpecificFiles("file1.txt", "file2.pdf")`
- **Manejo inteligente de duplicados**: 4 estrategias disponibles
- **Múltiples algoritmos de comparación**: Desde rápido hasta preciso
- **API fluida extendida**: 10+ nuevos métodos de configuración
- **Mejor logging**: Información detallada sobre decisiones de copia

#### 🔧 Mejoras
- **Rendimiento optimizado**: Verificación inteligente antes de copiar
- **Flexibilidad aumentada**: Combinación de patrones y archivos específicos
- **Retrocompatibilidad**: Todos los métodos existentes funcionan igual

#### 🐛 Correcciones
- Mejorada la gestión de memoria en operaciones de hash
- Optimizada la generación de nombres únicos
- Corregida la detección de archivos duplicados en rutas largas

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
- Consulta los logs para información detallada de errores
- Verifica la sección de solución de problemas

## 🎯 Roadmap Futuro

- **v1.3.0**: Soporte para filtros de contenido por expresiones regulares
- **v1.4.0**: Integración con servicios en la nube (Azure, AWS, Google Drive)
- **v1.5.0**: Interface gráfica opcional para configuración
- **v1.6.0**: Compresión automática de archivos durante la copia
- **v1.7.0**: Sincronización bidireccional inteligente
