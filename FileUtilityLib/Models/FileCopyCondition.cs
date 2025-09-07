
namespace FileUtilityLib.Models
{
    public class FileCopyCondition
    {
        public ConditionType Type { get; set; }
        public object? Value { get; set; }
        public DateTime? DateValue => Value as DateTime?;
        public long? SizeValue => Value as long?;
        public string? StringValue => Value?.ToString();
    }
}
