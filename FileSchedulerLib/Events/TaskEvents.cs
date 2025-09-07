using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSchedulerLib.Events
{
    public class TaskEventArgs : EventArgs
    {
        public string TaskName { get; }
        public TaskEventArgs(string taskName) => TaskName = taskName;
    }

    public delegate void TaskStartedEventHandler(object sender, TaskEventArgs e);
    public delegate void TaskCompletedEventHandler(object sender, TaskEventArgs e);
    public delegate void TaskFailedEventHandler(object sender, TaskEventArgs e, Exception ex);
}
