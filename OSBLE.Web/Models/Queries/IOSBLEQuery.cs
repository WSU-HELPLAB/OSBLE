using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OSBLE.Models.Queries
{
    public interface IOSBLEQuery<out T>
    {
        IEnumerable<T> Execute();
    }
}