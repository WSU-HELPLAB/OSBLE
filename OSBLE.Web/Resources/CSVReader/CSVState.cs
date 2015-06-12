using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OSBLE.Resources.CSVReader
{
    //State interface
    public interface CSVState
    {
        void Handle();
    }
}