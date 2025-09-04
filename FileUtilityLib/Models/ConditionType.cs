
namespace FileUtilityLib.Models
{
    public enum ConditionType
    {
        ModifiedToday,
        ModifiedSince,
        CreatedToday,
        CreatedSince,
        FileSizeGreaterThan,
        FileSizeLessThan,
        FileExtension,
        FileName
    }
}
