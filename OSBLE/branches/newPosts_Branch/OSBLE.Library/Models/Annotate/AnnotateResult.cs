using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OSBLE.Models.Annotate
{
    public enum ResultCode { OK, ERROR }
    public class AnnotateResult
    {
        public ResultCode Result { get; set; }
        public string RawMessage { get; set; }
        public string DocumentCode { get; set; }
        public string DocumentDate { get; set; }
    }
}
