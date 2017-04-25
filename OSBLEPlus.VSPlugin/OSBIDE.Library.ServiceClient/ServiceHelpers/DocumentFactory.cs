using System;
using System.IO;
using EnvDTE;
using EnvDTE80;

using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;

namespace OSBIDE.Library.ServiceClient.ServiceHelpers
{
    public class DocumentFactory
    {
        //kind of silly right now, but it does set us up nicely for future expansion
        public static CodeDocument FromDteDocument(Document document)
        {
            var codeDocument = new CodeDocument
            {
                FileName = Path.Combine(document.Path, document.Name),
                Content = File.ReadAllText(document.FullName)
            };

            var dte = (DTE2)document.DTE;
            //start at 1 when iterating through Error List
            for (var i = 1; i <= dte.ToolWindows.ErrorList.ErrorItems.Count; i++)
            {
                var item = dte.ToolWindows.ErrorList.ErrorItems.Item(i);

                //only grab events that are related to the current file
                var fileName = Path.GetFileName(item.FileName);
                if (fileName == null) continue;

                var itemFileName = fileName.ToLower();
                var name = Path.GetFileName(document.FullName);
                if (name == null) continue;

                var documentFileName = name.ToLower();
                if (string.Compare(itemFileName, documentFileName, StringComparison.CurrentCultureIgnoreCase) == 0)
                {
                    var eli = new CodeDocumentErrorListItem
                    {
                        CodeDocument = codeDocument,
                        ErrorListItem = TypeConverters.ErrorItemToErrorListItem(item)
                    };
                    codeDocument.ErrorItems.Add(eli);
                }
            }

            //add in breakpoint information
            for (var i = 1; i <= dte.Debugger.Breakpoints.Count; i++)
            {
                var bp = TypeConverters.IdeBreakPointToBreakPoint(dte.Debugger.Breakpoints.Item(i));

                //agan, only grab breakpoints set in the current document
                if (string.Compare(bp.File, document.FullName, StringComparison.OrdinalIgnoreCase) != 0) continue;

                var cbp = new CodeDocumentBreakPoint
                {
                    CodeDocument = codeDocument,
                    BreakPoint = bp
                };
                codeDocument.BreakPoints.Add(cbp);
            }
            return codeDocument;
        }
    }
}
