using System.Collections.Generic;

namespace OSBLEPlus.Logic.DomainObjects.ActivityFeeds
{
    public sealed class StackFrame
    {
        public int Id { get; set; }

        public string FunctionName { get; set; }

        public string Module { get; set; }

        public string Language { get; set; }

       public string FileName { get; set; }

        public int ExceptionEventId { get; set; }

        public string ReturnType { get; set; }

        public int LineNumber { get; set; }

        public ExceptionEvent Exception { get; set; }

        /// <summary>
        /// The depth of the stack frame.  A depth of 0 means that it is the top
        /// most stack frame.
        /// </summary>
        public int Depth { get; set; }

        public IList<StackFrameVariable> Variables { get; set; }

        public StackFrame()
        {
            Variables = new List<StackFrameVariable>();
        }
    }
}
