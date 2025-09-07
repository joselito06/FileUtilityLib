using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSchedulerLib.Core
{
    public interface ITaskCondition
    {
        bool ShouldRun();
    }
}
