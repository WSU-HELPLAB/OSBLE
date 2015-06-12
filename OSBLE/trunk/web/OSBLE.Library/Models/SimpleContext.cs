using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OSBLE.Models
{
    class SimpleContext : ContextBase
    {
        public SimpleContext()
            : base("OSBLEData")
        {
        }
    }
}
