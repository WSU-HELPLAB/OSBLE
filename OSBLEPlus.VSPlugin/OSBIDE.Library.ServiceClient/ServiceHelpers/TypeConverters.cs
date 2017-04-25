using System;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using EnvDTE90a;
using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;
using StackFrame = OSBLEPlus.Logic.DomainObjects.ActivityFeeds.StackFrame;

namespace OSBIDE.Library.ServiceClient.ServiceHelpers
{
    public static class TypeConverters
    {
        public static ErrorListItem ErrorItemToErrorListItem(ErrorItem item)
        {
            var eli = new ErrorListItem();

            //the original comment from AC
            //Sometimes ErrorItem references are invalid.  Not sure why.
            try
            {
                eli.Project = item.Project;
                eli.Column = item.Column;
                eli.Line = item.Line;
                eli.File = item.FileName;
                eli.Description = item.Description;
            }
            catch (Exception)
            {
                // ignored
            }

            return eli;
        }

        public static BreakPoint IdeBreakPointToBreakPoint(Breakpoint breakPoint)
        {
            return new BreakPoint
            {
                Condition = breakPoint.Condition,
                File = breakPoint.File,
                FileColumn = breakPoint.FileColumn,
                FileLine = breakPoint.FileLine,
                FunctionColumnOffset = breakPoint.FunctionColumnOffset,
                FunctionLineOffset = breakPoint.FunctionLineOffset,
                FunctionName = breakPoint.FunctionName,
                Name = breakPoint.Name,
                Enabled = breakPoint.Enabled,
            };
        }

        public static StackFrame VsStackFrameToStackFrame(EnvDTE.StackFrame frame)
        {
            var frame2 = (StackFrame2) frame;

            if (string.IsNullOrWhiteSpace(frame2.FileName))
            {
                return new StackFrame();
            }

            var stackFrame = new StackFrame
            {
                LineNumber = (int)frame2.LineNumber,
                FileName = frame2.FileName,
                FunctionName = frame.FunctionName,
                Module = frame.Module,
                Language = frame.Language,
                ReturnType = frame.ReturnType
            };

            foreach (var var in from Expression local in frame.Locals select new StackFrameVariable
                                                                                {
                                                                                    Name = local.Name,
                                                                                    Value = local.Value
                                                                                })
            {
                stackFrame.Variables.Add(var);
            }

            return stackFrame;
        }
    }
}
