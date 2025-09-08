using FileUtilityLib.Core.Interfaces;
using FileUtilityLib.Models.Events;
using FileUtilityLib.Models;
using Microsoft.Extensions.Logging;
using FileUtilityLib.Core.Compatibility;

namespace FileUtilityLib.Core.Services
{
    public class FileCopyService : IFileCopyService
    {
        private readonly ILogger<FileCopyService> _logger;
        private readonly ITaskManager _taskManager;

        public event EventHandler<CopyOperationEventArgs>? OperationStarted;
        public event EventHandler<CopyOperationEventArgs>? OperationCompleted;
        public event EventHandler<FileOperationEventArgs>? FileProcessing;
        public event EventHandler<FileOperationEventArgs>? FileProcessed;

        public FileCopyService(ILogger<FileCopyService> logger, ITaskManager taskManager)
        {
            _logger = logger;
            _taskManager = taskManager;
        }

        public async Task<CopyOperationResult> ExecuteTaskAsync(FileCopyTask task, CancellationToken cancellationToken = default)
        {
            var result = new CopyOperationResult
            {
                TaskId = task.Id,
                TaskName = task.Name,
                Status = CopyStatus.InProgress,
                StartTime = DateTime.Now
            };

            try
            {
                _logger.LogInformation("Iniciando tarea de copia: {TaskName} (ID: {TaskId})", task.Name, task.Id);
                OperationStarted?.Invoke(this, new CopyOperationEventArgs(result));

                // Validar rutas
                if (!Directory.Exists(task.SourcePath))
                {
                    result.Status = CopyStatus.Failed;
                    result.GeneralError = $"La ruta origen no existe: {task.SourcePath}";
                    result.EndTime = DateTime.Now;
                    return result;
                }

                // Obtener archivos que cumplen las condiciones
                var filesToCopy = GetFilesToCopy(task);
                result.TotalFiles = filesToCopy.Count;

                _logger.LogInformation("Encontrados {FileCount} archivos para copiar", filesToCopy.Count);

                if (filesToCopy.Count == 0)
                {
                    result.Status = CopyStatus.Completed;
                    result.EndTime = DateTime.Now;
                    _logger.LogInformation("No se encontraron archivos que cumplan las condiciones");
                    return result;
                }

                // Crear directorios de destino si no existen
                foreach (var destPath in task.DestinationPaths)
                {
                    try
                    {
                        Directory.CreateDirectory(destPath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error creando directorio de destino: {DestPath}", destPath);
                    }
                }

                // Copiar archivos
                foreach (var sourceFile in filesToCopy)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var fileName = Path.GetFileName(sourceFile);
                    var fileInfo = new FileInfo(sourceFile);

                    foreach (var destPath in task.DestinationPaths)
                    {
                        var destFile = Path.Combine(destPath, fileName);
                        var fileResult = new FileOperationResult
                        {
                            FilePath = sourceFile,
                            DestinationPath = destFile,
                            FileSizeBytes = fileInfo.Length,
                            Timestamp = DateTime.Now
                        };

                        try
                        {
                            FileProcessing?.Invoke(this, new FileOperationEventArgs(fileResult));

                            // ✅ NUEVO: Verificar si debe copiarse (duplicados)
                            var shouldCopy = ShouldCopyFile(sourceFile, destFile, task);

                            if (!shouldCopy.ShouldCopy)
                            {
                                _logger.LogDebug("⏭️ Saltando archivo (duplicado): {FileName} - {Reason}", fileName, shouldCopy.Reason);
                                fileResult.Success = true; // Consideramos exitoso el saltar duplicados
                                result.SuccessfulFiles++;
                                result.FileResults.Add(fileResult);
                                FileProcessed?.Invoke(this, new FileOperationEventArgs(fileResult));
                                continue;
                            }

                            // Determinar archivo final de destino (puede renombrarse)
                            var finalDestFile = shouldCopy.FinalDestinationPath ?? destFile;
                            fileResult.DestinationPath = finalDestFile;

                            await CopyFileAsync(sourceFile, destFile, cancellationToken);

                            fileResult.Success = true;
                            result.SuccessfulFiles++;

                            _logger.LogDebug("Archivo copiado: {SourceFile} -> {DestFile}", sourceFile, destFile);
                        }
                        catch (Exception ex)
                        {
                            fileResult.Success = false;
                            fileResult.ErrorMessage = ex.Message;
                            result.FailedFiles++;

                            _logger.LogError(ex, "Error copiando archivo: {SourceFile} -> {DestFile}", sourceFile, destFile);
                        }

                        result.FileResults.Add(fileResult);
                        FileProcessed?.Invoke(this, new FileOperationEventArgs(fileResult));
                    }
                }

                // Determinar estado final
                if (result.FailedFiles == 0)
                {
                    result.Status = CopyStatus.Completed;
                }
                else if (result.SuccessfulFiles > 0)
                {
                    result.Status = CopyStatus.PartialSuccess;
                }
                else
                {
                    result.Status = CopyStatus.Failed;
                }

                result.EndTime = DateTime.Now;

                // Actualizar última ejecución
                task.LastExecuted = result.EndTime;
                _taskManager.UpdateTask(task);

                _logger.LogInformation(
                    "Tarea completada: {TaskName}. Exitosos: {Success}, Fallidos: {Failed}, Duración: {Duration}",
                    task.Name, result.SuccessfulFiles, result.FailedFiles, result.Duration);
            }
            catch (OperationCanceledException)
            {
                result.Status = CopyStatus.Failed;
                result.GeneralError = "Operación cancelada";
                result.EndTime = DateTime.Now;

                _logger.LogWarning("Tarea cancelada: {TaskName}", task.Name);
            }
            catch (Exception ex)
            {
                result.Status = CopyStatus.Failed;
                result.GeneralError = ex.Message;
                result.EndTime = DateTime.Now;

                _logger.LogError(ex, "Error ejecutando tarea: {TaskName}", task.Name);
            }
            finally
            {
                OperationCompleted?.Invoke(this, new CopyOperationEventArgs(result));
            }

            return result;
        }

        public async Task<CopyOperationResult> ExecuteTaskAsync(string taskId, CancellationToken cancellationToken = default)
        {
            var task = _taskManager.GetTask(taskId);
            if (task == null)
            {
                return new CopyOperationResult
                {
                    TaskId = taskId,
                    Status = CopyStatus.Failed,
                    StartTime = DateTime.Now,
                    EndTime = DateTime.Now,
                    GeneralError = $"Tarea no encontrada: {taskId}"
                };
            }

            return await ExecuteTaskAsync(task, cancellationToken);
        }

        public List<string> GetFilesToCopy(FileCopyTask task)
        {
            var files = new List<string>();

            try
            {
                // ✅ NUEVO: Prioridad a archivos específicos
                if (task.SpecificFiles.Count > 0)
                {
                    _logger.LogInformation("🎯 Buscando {Count} archivos específicos", task.SpecificFiles.Count);

                    foreach (var fileName in task.SpecificFiles)
                    {
                        var filePath = Path.Combine(task.SourcePath, fileName);
                        if (File.Exists(filePath))
                        {
                            files.Add(filePath);
                            _logger.LogDebug("✓ Encontrado archivo específico: {FileName}", fileName);
                        }
                        else
                        {
                            _logger.LogWarning("⚠️ Archivo específico no encontrado: {FileName}", fileName);
                        }
                    }
                }
                else
                {
                    // Obtener archivos según los patrones
                    foreach (var pattern in task.FilePatterns)
                    {
                        var patternFiles = Directory.GetFiles(task.SourcePath, pattern, SearchOption.TopDirectoryOnly);
                        files.AddRange(patternFiles);
                    }

                    // Si no hay patrones específicos, obtener todos los archivos
                    if (task.FilePatterns.Count == 0)
                    {
                        files.AddRange(Directory.GetFiles(task.SourcePath, "*.*", SearchOption.TopDirectoryOnly));
                    }
                }
                
                // Eliminar duplicados
                files = files.Distinct().ToList();

                // Filtrar por condiciones
                if (task.Conditions.Count > 0)
                {
                    files = files.Where(file => EvaluateConditions(file, task.Conditions)).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo archivos de: {SourcePath}", task.SourcePath);
            }

            return files;
        }

        public bool EvaluateConditions(string filePath, List<FileCopyCondition> conditions)
        {
            if (!File.Exists(filePath)) return false;

            var fileInfo = new FileInfo(filePath);

            foreach (var condition in conditions)
            {
                switch (condition.Type)
                {
                    case ConditionType.ModifiedToday:
                        if (fileInfo.LastWriteTime.Date != DateTime.Today)
                            return false;
                        break;

                    case ConditionType.ModifiedSince:
                        if (condition.DateValue.HasValue && fileInfo.LastWriteTime < condition.DateValue.Value)
                            return false;
                        break;

                    case ConditionType.CreatedToday:
                        if (fileInfo.CreationTime.Date != DateTime.Today)
                            return false;
                        break;

                    case ConditionType.CreatedSince:
                        if (condition.DateValue.HasValue && fileInfo.CreationTime < condition.DateValue.Value)
                            return false;
                        break;

                    case ConditionType.FileSizeGreaterThan:
                        if (condition.SizeValue.HasValue && fileInfo.Length <= condition.SizeValue.Value)
                            return false;
                        break;

                    case ConditionType.FileSizeLessThan:
                        if (condition.SizeValue.HasValue && fileInfo.Length >= condition.SizeValue.Value)
                            return false;
                        break;

                    case ConditionType.FileExtension:
                        if (!string.IsNullOrEmpty(condition.StringValue))
                        {
                            var extension = Path.GetExtension(filePath).TrimStart('.');
                            var conditionExt = condition.StringValue?.TrimStart('.');
                            if (!extension.Equals(conditionExt, StringComparison.OrdinalIgnoreCase))
                                return false;
                        }
                        break;

                    case ConditionType.FileName:
                        if (!string.IsNullOrEmpty(condition.StringValue))
                        {
                            var fileName = Path.GetFileNameWithoutExtension(filePath);
                            if (!fileName.ToLower().Contains(condition.StringValue?.ToLower() ?? string.Empty))
                                return false;
                        }
                        break;
                }
            }

            return true;
        }

        /// <summary>
        /// Copia un archivo de forma asíncrona compatible con todos los frameworks
        /// </summary>
        private static async Task CopyFileAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken)
        {
            const int bufferSize = 4096;
            using var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, FileOptions.SequentialScan);
            using var destinationStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, FileOptions.SequentialScan);

            //await sourceStream.CopyToAsync(destinationStream, bufferSize, cancellationToken);
            // Usar método compatible
            await FrameworkCompatibility.CopyToAsync(sourceStream, destinationStream, bufferSize, cancellationToken);
        }
        
        // ✅ NUEVO: Comparar si dos archivos son iguales
        private bool AreFilesEqual(FileInfo sourceInfo, FileInfo destInfo, DuplicateComparison comparison)
        {
            switch (comparison)
            {
                case DuplicateComparison.SizeAndDate:
                    return sourceInfo.Length == destInfo.Length &&
                           sourceInfo.LastWriteTime == destInfo.LastWriteTime;

                case DuplicateComparison.SizeOnly:
                    return sourceInfo.Length == destInfo.Length;

                case DuplicateComparison.DateOnly:
                    return sourceInfo.LastWriteTime == destInfo.LastWriteTime;

                case DuplicateComparison.HashContent:
                    return AreFilesEqualByHash(sourceInfo.FullName, destInfo.FullName);

                default:
                    return sourceInfo.Length == destInfo.Length &&
                           sourceInfo.LastWriteTime == destInfo.LastWriteTime;
            }
        }

        /// <summary>
        /// Comparar archivos por hash SHA-256 compatible con todos los frameworks
        /// </summary>
        // ✅ NUEVO: Comparar archivos por hash SHA-256
        private bool AreFilesEqualByHash(string file1, string file2)
        {
            try
            {
#if NET6_0_OR_GREATER || NET7_0_OR_GREATER || NET8_0_OR_GREATER
                using var sha256 = System.Security.Cryptography.SHA256.Create();
#else
                // .NET Framework - Implementación compatible
                using var sha256 = System.Security.Cryptography.SHA256Managed.Create();
#endif
                byte[] hash1, hash2;

                using (var stream1 = File.OpenRead(file1))
                {
                    hash1 = sha256.ComputeHash(stream1);
                }

                using (var stream2 = File.OpenRead(file2))
                {
                    hash2 = sha256.ComputeHash(stream2);
                }
#if NET6_0_OR_GREATER || NET7_0_OR_GREATER || NET8_0_OR_GREATER
                return hash1.SequenceEqual(hash2);
#elif !NETSTANDARD
                // .NET Framework - Comparación manual
                return CompareByteArrays(hash1, hash2);
#else
                return false;
#endif
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error comparando archivos por hash: {File1} vs {File2}", file1, file2);
                // Fallback a comparación por tamaño y fecha
                var info1 = new FileInfo(file1);
                var info2 = new FileInfo(file2);
                return info1.Length == info2.Length && info1.LastWriteTime == info2.LastWriteTime;
            }
        }
#if NETFRAMEWORK || NETSTANDARD2_0
        /// <summary>
        /// Comparación de arrays para frameworks antiguos
        /// </summary>
        private static bool CompareByteArrays(byte[] array1, byte[] array2)
        {
            if (array1.Length != array2.Length)
                return false;

            for (int i = 0; i < array1.Length; i++)
            {
                if (array1[i] != array2[i])
                    return false;
            }

            return true;
        }
#endif
        // ✅ NUEVO: Generar nombre único para archivo
        private string GenerateUniqueFileName(string originalPath)
        {
            var directory = Path.GetDirectoryName(originalPath) ?? string.Empty;
            var nameWithoutExt = Path.GetFileNameWithoutExtension(originalPath);
            var extension = Path.GetExtension(originalPath);

            var counter = 1;
            string newPath;

            do
            {
                var newName = $"{nameWithoutExt}_({counter}){extension}";
                newPath = Path.Combine(directory, newName);
                counter++;
            }
            while (File.Exists(newPath) && counter < 1000); // Máximo 1000 intentos

            return newPath;
        }

        // ✅ NUEVO: Verificar si un archivo debe copiarse
        private (bool ShouldCopy, string? Reason, string? FinalDestinationPath) ShouldCopyFile(string sourceFile, string destFile, FileCopyTask task)
        {
            // Si el archivo no existe en destino, siempre copiar
            if (!File.Exists(destFile))
            {
                return (true, "Archivo no existe en destino", destFile);
            }

            var sourceInfo = new FileInfo(sourceFile);
            var destInfo = new FileInfo(destFile);

            // Según el comportamiento configurado
            switch (task.DuplicateHandling)
            {
                case DuplicateHandling.Overwrite:
                    return (true, "Sobrescribir siempre", destFile);

                case DuplicateHandling.Skip:
                    if (AreFilesEqual(sourceInfo, destInfo, task.DuplicateComparison))
                    {
                        return (false, $"Archivo idéntico (comparación: {task.DuplicateComparison})", null);
                    }
                    return (true, "Archivos diferentes", destFile);

                case DuplicateHandling.OverwriteIfNewer:
                    if (sourceInfo.LastWriteTime > destInfo.LastWriteTime)
                    {
                        return (true, "Archivo origen más nuevo", destFile);
                    }
                    return (false, "Archivo destino igual o más nuevo", null);

                case DuplicateHandling.RenameNew:
                    if (AreFilesEqual(sourceInfo, destInfo, task.DuplicateComparison))
                    {
                        return (false, "Archivo idéntico, no renombrar", null);
                    }
                    else
                    {
                        // Generar nombre único
                        var newName = GenerateUniqueFileName(destFile);
                        return (true, "Renombrar archivo", newName);
                    }

                default:
                    return (true, "Comportamiento por defecto", destFile);
            }
        }
    }
}
