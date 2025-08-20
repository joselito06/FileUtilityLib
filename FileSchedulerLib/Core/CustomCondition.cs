using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSchedulerLib.Core
{
    public class CustomCondition : ITaskCondition
    {
        private readonly Func<bool> _predicate;

        public CustomCondition(Func<bool> predicate)
        {
            _predicate = predicate;
        }

        public bool ShouldRun() => _predicate();
    }
}
