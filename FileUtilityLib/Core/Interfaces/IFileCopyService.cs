using FileUtilityLib.Models.Events;
using FileUtilityLib.Models;

namespace FileUtilityLib.Core.Interfaces
{
    public interface IFileCopyService
    {
        event EventHandler<CopyOperationEventArgs>? OperationStarted;
        event EventHandler<CopyOperationEventArgs>? OperationCompleted;
        event EventHandler<FileOperationEventArgs>? FileProcessing;
        event EventHandler<FileOperationEventArgs>? FileProcessed;

        Task<CopyOperationResult> ExecuteTaskAsync(FileCopyTask task, CancellationToken cancellationToken = default);
        Task<CopyOperationResult> ExecuteTaskAsync(string taskId, CancellationToken cancellationToken = default);
        List<string> GetFilesToCopy(FileCopyTask task);
        bool EvaluateConditions(string filePath, List<FileCopyCondition> conditions);
    }
}
