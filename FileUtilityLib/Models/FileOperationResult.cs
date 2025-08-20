
namespace FileUtilityLib.Models
{
    public class FileOperationResult
    {
        public bool Success { get; set; }
        public List<string> CopiedFiles { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }
}
