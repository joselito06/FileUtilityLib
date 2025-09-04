using FileUtilityLib.Core.Interfaces;
using FileUtilityLib.Models.Events;
using FileUtilityLib.Models;
using Microsoft.Extensions.Logging;

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
                            var conditionExt = condition.StringValue.TrimStart('.');
                            if (!extension.Equals(conditionExt, StringComparison.OrdinalIgnoreCase))
                                return false;
                        }
                        break;

                    case ConditionType.FileName:
                        if (!string.IsNullOrEmpty(condition.StringValue))
                        {
                            var fileName = Path.GetFileNameWithoutExtension(filePath);
                            if (!fileName.Contains(condition.StringValue, StringComparison.OrdinalIgnoreCase))
                                return false;
                        }
                        break;
                }
            }

            return true;
        }

        private static async Task CopyFileAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken)
        {
            const int bufferSize = 4096;
            using var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, FileOptions.SequentialScan);
            using var destinationStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, FileOptions.SequentialScan);

            await sourceStream.CopyToAsync(destinationStream, bufferSize, cancellationToken);
        }
    }
}
