
namespace FileUtilityLib.Models
{
    public class FileOperationResult
    {
        public string FilePath { get; set; } = string.Empty;
        public string DestinationPath { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public long FileSizeBytes { get; set; }
    }
}
