
namespace FileUtilityLib.Models.Events
{
    public class FileOperationEventArgs : EventArgs
    {
        public FileOperationResult Result { get; }
        public FileOperationEventArgs(FileOperationResult result)
        {
            Result = result;
        }
    }
}
