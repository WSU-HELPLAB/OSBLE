using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using EnvDTE;
using EnvDTE80;
using Ionic.Zip;
using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;
using OSBLEPlus.Logic.DomainObjects.Interface;
using OSBLEPlus.Logic.Utility;

namespace OSBIDE.Library.ServiceClient.ServiceHelpers
{
    public class EventFactory
    {
        //position of strings must match position in DebugActions enumeration
        private static readonly List<string> DebugCommands =
            Enum.GetNames(typeof(DebugActions)).Select(x => string.Format("Debug.{0}", x)).ToList();

        private static readonly List<string> CutCopyPasteCommands =
            Enum.GetNames(typeof(CutCopyPasteActions)).Select(x => string.Format("Edit.{0}", x)).ToList();

        public static IActivityEvent FromCommand(string commandName, DTE2 dte)
        {
            IActivityEvent oEvent = null;

            //debugging events
            if (DebugCommands.Contains(commandName))
            {
                var action = (DebugActions)Enum.Parse(typeof(DebugActions), commandName.Split('.')[1]);
                var debug = new DebugEvent
                {
                    SolutionName = dte.Solution.FullName,
                };

                //sometimes document name can be null
                try
                {
                    debug.DocumentName = dte.ActiveDocument.Name;
                }
                catch (Exception)
                {
                    debug.DocumentName = dte.Solution.FullName;
                }

                //add line number if applicable
                if (action == DebugActions.StepInto
                    || action == DebugActions.StepOut
                    || action == DebugActions.StepOver
                    )
                {
                    //line number can be null if there is no document open
                    try
                    {
                        TextSelection debugSelection = dte.ActiveDocument.Selection;
                        debugSelection.SelectLine();
                        var lineNumber = debugSelection.CurrentLine;
                        debug.LineNumber = lineNumber;
                        debug.DebugOutput = debugSelection.Text;
                    }
                    catch (Exception)
                    {
                        debug.LineNumber = 0;
                    }
                }

                //kind of reappropriating this for our current use.  Consider refactoring.
                debug.ExecutionAction = (int)action;

                //throw the content of the output window into the event if we just stopped debugging
                if (action == DebugActions.StopDebugging)
                {
                    var debugWindow = dte.ToolWindows.OutputWindow.OutputWindowPanes.Item("Debug");
                    if (debugWindow != null)
                    {
                        var text = debugWindow.TextDocument;
                        var selection = text.Selection;
                        selection.StartOfDocument();
                        selection.EndOfDocument(true);
                        debug.DebugOutput = selection.Text;
                        selection.EndOfDocument();
                    }
                }

                oEvent = debug;
            }
            else if (CutCopyPasteCommands.Contains(commandName))
            {
                var ccp = new CutCopyPasteEvent
                {
                    SolutionName = dte.Solution.FullName,
                    EventActionId = (int) Enum.Parse(typeof (CutCopyPasteActions), commandName.Split('.')[1]),
                    Content = Clipboard.GetText()
                };
                //sometimes document name can be null
                try
                {
                    ccp.DocumentName = dte.ActiveDocument.Name;
                }
                catch (Exception)
                {
                    ccp.DocumentName = dte.Solution.FullName;
                }
                oEvent = ccp;
            }

            return oEvent;
        }

        /// <summary>
        /// Converts a zipped, binary format of IOsbideEvent back into object form
        /// </summary>
        /// <param name="data"></param>
        /// <param name="binder"></param>
        /// <returns></returns>
        public static ActivityEvent FromZippedBinary(byte[] data, SerializationBinder binder = null)
        {
            var zippedStream = new MemoryStream(data);
            var rawStream = new MemoryStream();
            var formatter = new BinaryFormatter();

            //unzip the memory stream
            using (var zip = ZipFile.Read(zippedStream))
            {
                if (zip.Entries.Count == 1)
                {
                    var entry = zip.Entries.ElementAt(0);
                    entry.Extract(rawStream);
                    rawStream.Position = 0;
                }
                else
                {
                    throw new Exception("Expecting a zip file with exactly one item.");
                }
            }

            if (binder != null)
            {
                formatter.Binder = binder;
            }

            //figure out what needs to be deserialized
            var evt = (ActivityEvent)formatter.Deserialize(rawStream);
            return evt;
        }

        /// <summary>
        /// Converts the supplied IOsbideEvent into a zipped, binary format
        /// </summary>
        /// <param name="evt"></param>
        /// <returns></returns>
        public static byte[] ToZippedBinary(ActivityEvent evt)
        {
            var memStream = new MemoryStream();
            var zipStream = new MemoryStream();
            var serializer = new BinaryFormatter();
            serializer.Serialize(memStream, evt);

            //go back to position zero so that the zip file can read the memory stream
            memStream.Position = 0;

            //zip up to save space
            using (var zip = new ZipFile())
            {
                zip.AddEntry(evt.EventName, memStream);
                zip.Save(zipStream);
                zipStream.Position = 0;
            }
            return zipStream.ToArray();
        }
    }
}

