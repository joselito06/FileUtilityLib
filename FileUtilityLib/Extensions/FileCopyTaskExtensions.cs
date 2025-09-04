using FileUtilityLib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileUtilityLib.Extensions
{
    public static class FileCopyTaskExtensions
    {
        public static FileCopyTask AddDestination(this FileCopyTask task, string destinationPath)
        {
            task.DestinationPaths.Add(destinationPath);
            return task;
        }

        public static FileCopyTask AddDestinations(this FileCopyTask task, params string[] destinationPaths)
        {
            task.DestinationPaths.AddRange(destinationPaths);
            return task;
        }

        public static FileCopyTask AddFilePattern(this FileCopyTask task, string pattern)
        {
            task.FilePatterns.Add(pattern);
            return task;
        }

        public static FileCopyTask AddFilePatterns(this FileCopyTask task, params string[] patterns)
        {
            task.FilePatterns.AddRange(patterns);
            return task;
        }

        public static FileCopyTask AddCondition(this FileCopyTask task, ConditionType type, object? value = null)
        {
            task.Conditions.Add(new FileCopyCondition { Type = type, Value = value });
            return task;
        }

        public static FileCopyTask ModifiedToday(this FileCopyTask task)
        {
            return task.AddCondition(ConditionType.ModifiedToday);
        }

        public static FileCopyTask ModifiedSince(this FileCopyTask task, DateTime since)
        {
            return task.AddCondition(ConditionType.ModifiedSince, since);
        }

        public static FileCopyTask CreatedToday(this FileCopyTask task)
        {
            return task.AddCondition(ConditionType.CreatedToday);
        }

        public static FileCopyTask CreatedSince(this FileCopyTask task, DateTime since)
        {
            return task.AddCondition(ConditionType.CreatedSince, since);
        }

        public static FileCopyTask WithFileExtension(this FileCopyTask task, string extension)
        {
            return task.AddCondition(ConditionType.FileExtension, extension);
        }

        public static FileCopyTask ContainingFileName(this FileCopyTask task, string namePattern)
        {
            return task.AddCondition(ConditionType.FileName, namePattern);
        }

        public static FileCopyTask FileSizeGreaterThan(this FileCopyTask task, long sizeInBytes)
        {
            return task.AddCondition(ConditionType.FileSizeGreaterThan, sizeInBytes);
        }

        public static FileCopyTask FileSizeLessThan(this FileCopyTask task, long sizeInBytes)
        {
            return task.AddCondition(ConditionType.FileSizeLessThan, sizeInBytes);
        }

        public static FileCopyTask Enable(this FileCopyTask task)
        {
            task.IsEnabled = true;
            return task;
        }

        public static FileCopyTask Disable(this FileCopyTask task)
        {
            task.IsEnabled = false;
            return task;
        }
    }
}
