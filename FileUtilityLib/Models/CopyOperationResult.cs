
namespace FileUtilityLib.Models
{
    public class CopyOperationResult
    {
        public string TaskId { get; set; } = string.Empty;
        public string TaskName { get; set; } = string.Empty;
        public CopyStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public List<FileOperationResult> FileResults { get; set; } = new();
        public int TotalFiles { get; set; }
        public int SuccessfulFiles { get; set; }
        public int FailedFiles { get; set; }
        public string? GeneralError { get; set; }

        public TimeSpan Duration => EndTime.HasValue ? EndTime.Value - StartTime : TimeSpan.Zero;
        public bool IsCompleted => Status == CopyStatus.Completed || Status == CopyStatus.Failed || Status == CopyStatus.PartialSuccess;
    }
}
