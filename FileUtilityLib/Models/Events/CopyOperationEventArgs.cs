
namespace FileUtilityLib.Models.Events
{
    public class CopyOperationEventArgs : EventArgs
    {
        public CopyOperationResult Result { get; }
        public CopyOperationEventArgs(CopyOperationResult result)
        {
            Result = result;
        }
    }
}
