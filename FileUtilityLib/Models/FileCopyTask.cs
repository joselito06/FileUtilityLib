
namespace FileUtilityLib.Models
{
    public class FileCopyTask
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string SourcePath { get; set; } = string.Empty;
        public List<string> DestinationPaths { get; set; } = new();
        public List<string> FilePatterns { get; set; } = new(); // *.txt, specific files, etc.
        public List<string> SpecificFiles { get; set; } = new(); // ✅ NUEVO: Archivos específicos por nombre
        public List<FileCopyCondition> Conditions { get; set; } = new();
        public bool IsEnabled { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? LastExecuted { get; set; }

        // ✅ NUEVO: Configuración de duplicados
        public DuplicateHandling DuplicateHandling { get; set; } = DuplicateHandling.Skip;
        public DuplicateComparison DuplicateComparison { get; set; } = DuplicateComparison.SizeAndDate;
    }
}
