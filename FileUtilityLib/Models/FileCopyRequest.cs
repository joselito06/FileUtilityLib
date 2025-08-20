
namespace FileUtilityLib.Models
{
    public class FileCopyRequest
    {
        public string SourceFolder { get; set; } = string.Empty;
        public List<string> FilesToCopy { get; set; } = new();
        public List<string> DestinationFolders { get; set; } = new();
        public bool Overwrite { get; set; } = true;
    }
}
