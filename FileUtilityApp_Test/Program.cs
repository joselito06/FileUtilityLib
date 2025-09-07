
using FileUtilityLib.Core.Services;
using FileUtilityLib.Extensions;
using FileUtilityLib.Models;
using FileUtilityLib.Scheduler.Services;

internal class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== EJEMPLOS CON NUEVAS FUNCIONALIDADES ===\n");

        try
        {
            // Crear entorno de prueba mejorado
            await SetupEnhancedTestEnvironment();

            using var fileUtility = ServiceCollectionExtensions.CreateFileUtilityService(@"C:\FileUtilityTest\Config");

            // Configurar eventos
            ConfigureEvents(fileUtility);

            // Mostrar menú de ejemplos
            await ShowExamplesMenu(fileUtility);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
        }
    }

    private static async Task SetupEnhancedTestEnvironment()
    {
        Console.WriteLine("🔧 Configurando entorno mejorado...\n");

        var sourceDir = @"C:\FileUtilityTest\Source";
        var destDir = @"C:\FileUtilityTest\Destination";

        Directory.CreateDirectory(sourceDir);
        Directory.CreateDirectory(destDir);
        Directory.CreateDirectory(@"C:\FileUtilityTest\Config");

        // Crear archivos específicos para las pruebas
        var testFiles = new[]
        {
            // Archivos específicos que queremos copiar
            new { Name = "Prueba1.xlsx", Content = "Contenido del archivo Prueba1", Size = 2000 },
            new { Name = "Prueba2.xlsx", Content = "Contenido del archivo Prueba2", Size = 2500 },
            new { Name = "ReporteVentas.pdf", Content = "Reporte de ventas del mes", Size = 5000 },
            new { Name = "ConfiguracionSistema.json", Content = "{ \"setting\": \"value\" }", Size = 500 },
            
            // Otros archivos que NO queremos copiar
            new { Name = "ArchivoNoDeseado1.xlsx", Content = "Este no debe copiarse", Size = 1000 },
            new { Name = "ArchivoNoDeseado2.pdf", Content = "Este tampoco", Size = 1500 },
            new { Name = "temporal.tmp", Content = "Archivo temporal", Size = 300 },
            
            // Archivos para pruebas de duplicados
            new { Name = "Duplicado1.txt", Content = "Contenido para duplicar", Size = 800 },
            new { Name = "Archivo_Grande.dat", Content = new string('A', 10000), Size = 10000 }
        };

        foreach (var file in testFiles)
        {
            var filePath = Path.Combine(sourceDir, file.Name);
            var content = file.Content.Length > file.Size ?
                         file.Content.Substring(0, file.Size) :
                         file.Content.PadRight(file.Size, ' ');

            await File.WriteAllTextAsync(filePath, content);

            Console.WriteLine($"   📄 {file.Name} ({file.Size} bytes)");
        }

        // Crear algunos archivos en destino para probar duplicados
        await CreateDuplicateTestFiles(destDir);

        Console.WriteLine($"\n✅ Entorno mejorado creado:");
        Console.WriteLine($"   📂 Origen: {sourceDir}");
        Console.WriteLine($"   📂 Destino: {destDir}");
        Console.WriteLine($"   📄 Archivos: {testFiles.Length} archivos");
    }

    private static async Task CreateDuplicateTestFiles(string destDir)
    {
        Console.WriteLine("\n🔄 Creando archivos para pruebas de duplicados...");

        // Crear archivo idéntico
        var duplicadoPath = Path.Combine(destDir, "Duplicado1.txt");
        await File.WriteAllTextAsync(duplicadoPath, "Contenido para duplicar".PadRight(800, ' '));
        File.SetLastWriteTime(duplicadoPath, DateTime.Now.AddMinutes(-5)); // Hace 5 minutos

        // Crear archivo con mismo nombre pero diferente contenido
        var diferentePath = Path.Combine(destDir, "Prueba1.xlsx");
        await File.WriteAllTextAsync(diferentePath, "Contenido diferente".PadRight(1500, ' '));
        File.SetLastWriteTime(diferentePath, DateTime.Now.AddHours(-1)); // Hace 1 hora

        Console.WriteLine($"   📄 Duplicado1.txt (idéntico)");
        Console.WriteLine($"   📄 Prueba1.xlsx (diferente contenido y fecha)");
    }

    private static async Task ShowExamplesMenu(FileUtilityService fileUtility)
    {
        while (true)
        {
            Console.WriteLine("\n🧪 EJEMPLOS CON NUEVAS FUNCIONALIDADES:");
            Console.WriteLine("1️⃣  - Archivos específicos solamente");
            Console.WriteLine("2️⃣  - Saltar duplicados (Skip)");
            Console.WriteLine("3️⃣  - Sobrescribir si es más nuevo");
            Console.WriteLine("4️⃣  - Renombrar archivos duplicados");
            Console.WriteLine("5️⃣  - Comparación por contenido (hash)");
            Console.WriteLine("6️⃣  - Combinación: específicos + condiciones");
            Console.WriteLine("7️⃣  - Múltiples archivos específicos + múltiples destinos");
            Console.WriteLine("8️⃣  - Prueba completa de duplicados");
            Console.WriteLine("📊 - Ver archivos en origen y destino");
            Console.WriteLine("🧹 - Limpiar destino");
            Console.WriteLine("🚀 - Iniciar/Parar scheduler");
            Console.WriteLine("❌ - Salir");

            Console.Write("\n🎯 Selecciona un ejemplo: ");
            var input = Console.ReadKey().KeyChar;
            Console.WriteLine();

            try
            {
                switch (input)
                {
                    case '1': await Example1_SpecificFiles(fileUtility); break;
                    case '2': await Example2_SkipDuplicates(fileUtility); break;
                    case '3': await Example3_OverwriteIfNewer(fileUtility); break;
                    case '4': await Example4_RenameDuplicates(fileUtility); break;
                    case '5': await Example5_CompareByHash(fileUtility); break;
                    case '6': await Example6_SpecificWithConditions(fileUtility); break;
                    case '7': await Example7_MultipleSpecificMultipleDestinations(fileUtility); break;
                    case '8': await Example8_CompleteDuplicateTest(fileUtility); break;
                    case 's':
                    case 'S': await ShowFiles(); break;
                    case 'c':
                    case 'C': await CleanDestination(); break;
                    case 'r':
                    case 'R': await ToggleScheduler(fileUtility); break;
                    case 'x':
                    case 'X': return;
                    default: Console.WriteLine("❓ Opción no válida"); break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error ejecutando ejemplo: {ex.Message}");
            }
        }
    }

    // EJEMPLO 1: Solo archivos específicos
    private static async Task Example1_SpecificFiles(FileUtilityService fileUtility)
    {
        Console.WriteLine("\n🧪 EJEMPLO 1: Solo archivos específicos");
        Console.WriteLine("📋 Copiará ÚNICAMENTE: Prueba1.xlsx y Prueba2.xlsx");

        var task = new FileCopyTask
        {
            Name = "Solo Archivos Específicos",
            SourcePath = @"C:\FileUtilityTest\Source"
        }
        .AddDestination(@"C:\FileUtilityTest\Destination\Specific")
        .AddSpecificFiles("Prueba1.xlsx", "Prueba2.xlsx")  // ✅ NUEVO
        .SkipDuplicates()  // ✅ NUEVO
        .Enable();

        Directory.CreateDirectory(@"C:\FileUtilityTest\Destination\Specific");

        var taskId = await fileUtility.CreateTaskAsync(task);
        Console.WriteLine($"✅ Tarea creada: {task.Name}");

        // Ejecutar inmediatamente para ver resultado
        var result = await fileUtility.ExecuteTaskNowAsync(taskId);
        Console.WriteLine($"📊 Resultado: {result.Status}, Archivos: {result.SuccessfulFiles}/{result.TotalFiles}");
    }

    // EJEMPLO 2: Saltar duplicados
    private static async Task Example2_SkipDuplicates(FileUtilityService fileUtility)
    {
        Console.WriteLine("\n🧪 EJEMPLO 2: Saltar duplicados");
        Console.WriteLine("📋 NO copiará archivos que ya existan con mismo tamaño y fecha");

        var task = new FileCopyTask
        {
            Name = "Saltar Duplicados",
            SourcePath = @"C:\FileUtilityTest\Source"
        }
        .AddDestination(@"C:\FileUtilityTest\Destination")
        .AddFilePattern("*.txt")
        .SkipDuplicates()  // ✅ NUEVO
        .CompareBySizeAndDate()  // ✅ NUEVO
        .Enable();

        var taskId = await fileUtility.CreateTaskAsync(task);
        Console.WriteLine($"✅ Tarea creada: {task.Name}");

        var result = await fileUtility.ExecuteTaskNowAsync(taskId);
        Console.WriteLine($"📊 Resultado: {result.Status}, Archivos: {result.SuccessfulFiles}/{result.TotalFiles}");

        // Mostrar detalles de cada archivo
        foreach (var fileResult in result.FileResults)
        {
            var fileName = Path.GetFileName(fileResult.FilePath);
            var status = fileResult.Success ? "✓" : "✗";
            Console.WriteLine($"   {status} {fileName}");
        }
    }

    // EJEMPLO 3: Sobrescribir si es más nuevo
    private static async Task Example3_OverwriteIfNewer(FileUtilityService fileUtility)
    {
        Console.WriteLine("\n🧪 EJEMPLO 3: Sobrescribir si es más nuevo");
        Console.WriteLine("📋 Solo copiará si el archivo origen es más reciente");

        var task = new FileCopyTask
        {
            Name = "Sobrescribir Si Más Nuevo",
            SourcePath = @"C:\FileUtilityTest\Source"
        }
        .AddDestination(@"C:\FileUtilityTest\Destination")
        .AddSpecificFiles("Prueba1.xlsx", "Duplicado1.txt")
        .OverwriteIfNewer()  // ✅ NUEVO
        .Enable();

        var taskId = await fileUtility.CreateTaskAsync(task);
        Console.WriteLine($"✅ Tarea creada: {task.Name}");

        var result = await fileUtility.ExecuteTaskNowAsync(taskId);
        Console.WriteLine($"📊 Resultado: {result.Status}, Archivos: {result.SuccessfulFiles}/{result.TotalFiles}");
    }

    // EJEMPLO 4: Renombrar duplicados
    private static async Task Example4_RenameDuplicates(FileUtilityService fileUtility)
    {
        Console.WriteLine("\n🧪 EJEMPLO 4: Renombrar archivos duplicados");
        Console.WriteLine("📋 Si existe, creará archivo con nombre único (ej: archivo_(1).txt)");

        var task = new FileCopyTask
        {
            Name = "Renombrar Duplicados",
            SourcePath = @"C:\FileUtilityTest\Source"
        }
        .AddDestination(@"C:\FileUtilityTest\Destination")
        .AddSpecificFiles("Duplicado1.txt")
        .RenameIfExists()  // ✅ NUEVO
        .Enable();

        var taskId = await fileUtility.CreateTaskAsync(task);
        Console.WriteLine($"✅ Tarea creada: {task.Name}");

        var result = await fileUtility.ExecuteTaskNowAsync(taskId);
        Console.WriteLine($"📊 Resultado: {result.Status}, Archivos: {result.SuccessfulFiles}/{result.TotalFiles}");

        // Mostrar archivos creados
        foreach (var fileResult in result.FileResults)
        {
            if (fileResult.Success)
            {
                var finalName = Path.GetFileName(fileResult.DestinationPath);
                Console.WriteLine($"   ✓ Creado: {finalName}");
            }
        }
    }

    // EJEMPLO 5: Comparación por hash de contenido
    private static async Task Example5_CompareByHash(FileUtilityService fileUtility)
    {
        Console.WriteLine("\n🧪 EJEMPLO 5: Comparación por contenido (hash)");
        Console.WriteLine("📋 Compara contenido real de archivos, no solo tamaño/fecha");

        var task = new FileCopyTask
        {
            Name = "Comparar Por Contenido",
            SourcePath = @"C:\FileUtilityTest\Source"
        }
        .AddDestination(@"C:\FileUtilityTest\Destination")
        .AddFilePattern("*.dat")
        .SkipDuplicates()
        .CompareByContent()  // ✅ NUEVO - Más lento pero preciso
        .Enable();

        var taskId = await fileUtility.CreateTaskAsync(task);
        Console.WriteLine($"✅ Tarea creada: {task.Name}");
        Console.WriteLine("⏳ Comparación por hash puede ser más lenta...");

        var result = await fileUtility.ExecuteTaskNowAsync(taskId);
        Console.WriteLine($"📊 Resultado: {result.Status}, Archivos: {result.SuccessfulFiles}/{result.TotalFiles}");
    }

    // EJEMPLO 6: Archivos específicos + condiciones
    private static async Task Example6_SpecificWithConditions(FileUtilityService fileUtility)
    {
        Console.WriteLine("\n🧪 EJEMPLO 6: Archivos específicos + condiciones");
        Console.WriteLine("📋 Archivos específicos que ADEMÁS cumplan condiciones");

        var task = new FileCopyTask
        {
            Name = "Específicos Con Condiciones",
            SourcePath = @"C:\FileUtilityTest\Source"
        }
        .AddDestination(@"C:\FileUtilityTest\Destination\Conditional")
        .AddSpecificFiles("Prueba1.xlsx", "Prueba2.xlsx", "ReporteVentas.pdf")
        .FileSizeGreaterThan(1500)  // Y que sean mayores a 1.5KB
        .SkipDuplicates()
        .Enable();

        Directory.CreateDirectory(@"C:\FileUtilityTest\Destination\Conditional");

        var taskId = await fileUtility.CreateTaskAsync(task);
        Console.WriteLine($"✅ Tarea creada: {task.Name}");
        Console.WriteLine("📋 Solo copiará archivos específicos que sean >1.5KB");

        var result = await fileUtility.ExecuteTaskNowAsync(taskId);
        Console.WriteLine($"📊 Resultado: {result.Status}, Archivos: {result.SuccessfulFiles}/{result.TotalFiles}");
    }

    // EJEMPLO 7: Múltiples archivos específicos + múltiples destinos
    private static async Task Example7_MultipleSpecificMultipleDestinations(FileUtilityService fileUtility)
    {
        Console.WriteLine("\n🧪 EJEMPLO 7: Múltiples específicos + múltiples destinos");
        Console.WriteLine("📋 Varios archivos específicos copiados a varios destinos");

        var task = new FileCopyTask
        {
            Name = "Multi Específicos Multi Destinos",
            SourcePath = @"C:\FileUtilityTest\Source"
        }
        .AddDestinations(
            @"C:\FileUtilityTest\Destination\Backup1",
            @"C:\FileUtilityTest\Destination\Backup2",
            @"C:\FileUtilityTest\Destination\Backup3"
        )
        .AddSpecificFiles(
            "Prueba1.xlsx",
            "Prueba2.xlsx",
            "ReporteVentas.pdf",
            "ConfiguracionSistema.json"
        )
        .SkipDuplicates()
        .Enable();

        // Crear directorios
        Directory.CreateDirectory(@"C:\FileUtilityTest\Destination\Backup1");
        Directory.CreateDirectory(@"C:\FileUtilityTest\Destination\Backup2");
        Directory.CreateDirectory(@"C:\FileUtilityTest\Destination\Backup3");

        var taskId = await fileUtility.CreateTaskAsync(task);
        Console.WriteLine($"✅ Tarea creada: {task.Name}");
        Console.WriteLine($"📋 Copiando {task.SpecificFiles.Count} archivos a {task.DestinationPaths.Count} destinos");

        var result = await fileUtility.ExecuteTaskNowAsync(taskId);
        Console.WriteLine($"📊 Resultado: {result.Status}, Operaciones: {result.SuccessfulFiles}/{result.TotalFiles}");
    }

    // EJEMPLO 8: Prueba completa de duplicados
    private static async Task Example8_CompleteDuplicateTest(FileUtilityService fileUtility)
    {
        Console.WriteLine("\n🧪 EJEMPLO 8: Prueba completa de todos los tipos de duplicados");

        // Crear 4 tareas con diferentes comportamientos
        var tasks = new[]
        {
            new FileCopyTask { Name = "Test Skip" }.AddDestination(@"C:\FileUtilityTest\Destination\TestSkip")
                .AddSpecificFile("Duplicado1.txt").SkipDuplicates(),

            new FileCopyTask { Name = "Test Overwrite" }.AddDestination(@"C:\FileUtilityTest\Destination\TestOverwrite")
                .AddSpecificFile("Duplicado1.txt").OverwriteAlways(),

            new FileCopyTask { Name = "Test IfNewer" }.AddDestination(@"C:\FileUtilityTest\Destination\TestIfNewer")
                .AddSpecificFile("Duplicado1.txt").OverwriteIfNewer(),

            new FileCopyTask { Name = "Test Rename" }.AddDestination(@"C:\FileUtilityTest\Destination\TestRename")
                .AddSpecificFile("Duplicado1.txt").RenameIfExists()
        };

        foreach (var task in tasks)
        {
            task.SourcePath = @"C:\FileUtilityTest\Source";
            task.Enable();

            // Crear directorio
            Directory.CreateDirectory(task.DestinationPaths[0]);

            var taskId = await fileUtility.CreateTaskAsync(task);
            Console.WriteLine($"\n🔧 Ejecutando: {task.Name}");

            var result = await fileUtility.ExecuteTaskNowAsync(taskId);
            Console.WriteLine($"   📊 {result.Status}: {result.SuccessfulFiles}/{result.TotalFiles}");

            if (result.FileResults.Any())
            {
                var fileResult = result.FileResults[0];
                var finalName = Path.GetFileName(fileResult.DestinationPath);
                Console.WriteLine($"   📄 Archivo final: {finalName}");
            }
        }
    }

    private static async Task ShowFiles()
    {
        Console.WriteLine("\n📊 ARCHIVOS EN SISTEMA:");

        var sourceDir = @"C:\FileUtilityTest\Source";
        var destDir = @"C:\FileUtilityTest\Destination";

        // Archivos origen
        Console.WriteLine($"\n📂 ORIGEN ({sourceDir}):");
        if (Directory.Exists(sourceDir))
        {
            var sourceFiles = Directory.GetFiles(sourceDir);
            foreach (var file in sourceFiles)
            {
                var info = new FileInfo(file);
                Console.WriteLine($"   📄 {Path.GetFileName(file)} ({info.Length} bytes, {info.LastWriteTime:HH:mm:ss})");
            }
        }

        // Archivos destino
        Console.WriteLine($"\n📤 DESTINO ({destDir}):");
        if (Directory.Exists(destDir))
        {
            ShowDirectoryContents(destDir, "");
        }
    }

    private static void ShowDirectoryContents(string directory, string indent)
    {
        try
        {
            var dirs = Directory.GetDirectories(directory);
            var files = Directory.GetFiles(directory);

            foreach (var dir in dirs)
            {
                Console.WriteLine($"{indent}📁 {Path.GetFileName(dir)}/");
                ShowDirectoryContents(dir, indent + "  ");
            }

            foreach (var file in files)
            {
                var info = new FileInfo(file);
                Console.WriteLine($"{indent}📄 {Path.GetFileName(file)} ({info.Length} bytes, {info.LastWriteTime:HH:mm:ss})");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{indent}❌ Error: {ex.Message}");
        }
    }

    private static async Task CleanDestination()
    {
        Console.WriteLine("\n🧹 Limpiando directorio destino...");

        var destDir = @"C:\FileUtilityTest\Destination";
        if (Directory.Exists(destDir))
        {
            try
            {
                Directory.Delete(destDir, true);
                Directory.CreateDirectory(destDir);
                Console.WriteLine("✅ Directorio destino limpiado");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error limpiando: {ex.Message}");
            }
        }
    }

    private static async Task ToggleScheduler(FileUtilityService fileUtility)
    {
        if (fileUtility.IsSchedulerRunning)
        {
            Console.WriteLine("⏹️ Deteniendo scheduler...");
            await fileUtility.StopSchedulerAsync();
        }
        else
        {
            Console.WriteLine("🚀 Iniciando scheduler...");
            await fileUtility.StartSchedulerAsync();
        }

        Console.WriteLine($"📊 Estado: {(fileUtility.IsSchedulerRunning ? "Activo" : "Inactivo")}");
    }

    private static void ConfigureEvents(FileUtilityService fileUtility)
    {
        fileUtility.OperationCompleted += (sender, e) =>
        {
            var icon = e.Result.Status switch
            {
                CopyStatus.Completed => "✅",
                CopyStatus.PartialSuccess => "⚠️",
                CopyStatus.Failed => "❌",
                _ => "ℹ️"
            };
            Console.WriteLine($"{icon} [COMPLETADA] {e.Result.TaskName}");
        };

        fileUtility.FileProcessed += (sender, e) =>
        {
            var status = e.Result.Success ? "✓" : "✗";
            var fileName = Path.GetFileName(e.Result.FilePath);
            var destName = Path.GetFileName(e.Result.DestinationPath);

            if (fileName != destName)
            {
                Console.WriteLine($"   {status} {fileName} → {destName}");
            }
            else
            {
                Console.WriteLine($"   {status} {fileName}");
            }
        };
    }
}