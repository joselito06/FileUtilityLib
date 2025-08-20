using FileUtilityLib.Models;

namespace FileUtilityLib.Core
{
    public class CopyFilesTask : IFileTask
    {
        private readonly FileCopyRequest _request;

        public CopyFilesTask(FileCopyRequest request)
        {
            _request = request;
        }

        public async Task<FileOperationResult> ExecuteAsync(CancellationToken token)
        {
            return await Task.Run(() =>
            {
                var manager = new FileManager();
                return manager.CopyFiles(_request);
            }, token);
        }
    }
}
