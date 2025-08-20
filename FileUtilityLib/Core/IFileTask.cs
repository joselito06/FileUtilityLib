using FileUtilityLib.Models;

namespace FileUtilityLib.Core
{
    public interface IFileTask
    {
        Task<FileOperationResult> ExecuteAsync(CancellationToken token);
    }
}
