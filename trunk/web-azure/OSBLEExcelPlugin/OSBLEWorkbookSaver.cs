// Created 5-13-13 by Evan Olds for the OSBLE project at WSU
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Ionic.Zip;
using Microsoft.Office.Interop.Excel;
using OSBLEExcelPlugin.OSBLEAuthService;

namespace OSBLEExcelPlugin
{
    internal static class OSBLEWorkbookSaver
    {
        public class SaveResult
        {
            private string m_errorMessage;
            
            private bool m_success;

            /// <summary>
            /// Default constructor that initializes the object with a success value 
            /// of true and a null error message.
            /// </summary>
            public SaveResult()
            {
                m_success = true;
                m_errorMessage = null;
            }

            public SaveResult(bool success, string errorMessage)
            {
                m_success = success;
                m_errorMessage = errorMessage;
            }

            public string ErrorMessage
            {
                get { return m_errorMessage; }
            }

            public bool Success
            {
                get { return m_success; }
            }
        }

        public static SaveResult Save(string userName, string password, int courseID,
            Workbook wb)
        {
            // What needs to be done here:
            // 1. "Export" each sheet in the workbook to a CSV
            // 2. Package all CSVs in a zip
            // 3. Upload this zip to OSBLE through the web service

            string tempSaveDir = Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);
            if (!tempSaveDir.EndsWith("\\") &&
                !tempSaveDir.EndsWith("/"))
            {
                tempSaveDir += "\\";
            }

            // For each non-empty worksheet in the workbook we need to make a CSV
            ZipFile zf = new ZipFile();
            int sheetCount = 0;
            foreach (Worksheet ws in wb.Worksheets)
            {
                // We only want gradebook worksheets.
                // Check for a "#" in cell A1 before making a CSV
                var currentWorkSheet = Convert.ToString(ws.Range["A1"].Value);

                if (currentWorkSheet == null) // Cell is empty
                {
                }
                else if (currentWorkSheet.Contains("#")) //Cell contains #, continue...
                {
                    // Make an in-memory CSV for the worksheet
                    string csvTemp = WorksheetToCSVString(ws);
                    if (string.IsNullOrEmpty(csvTemp))
                    {
                        continue;
                    }

                    sheetCount++;
                    zf.AddEntry(ws.Name + ".csv", Encoding.UTF8.GetBytes(csvTemp));
                }
            }

            // If we didn't get any data then we can't upload to OSBLE
            if (0 == sheetCount)
            {
                return new SaveResult(false,
                    string.Format(
                        "Save attempt at {0} failed because no data could be obtained " +
                        "from the workbook.\r\nPlease make sure you have at least 2x2 " + 
                        "cells worth of grade data in one or more worksheets.", DateTime.Now));
            }

            // Save the zip to a memory stream
            MemoryStream ms = new MemoryStream();
            zf.Save(ms);
            zf.Dispose();
            zf = null;

            // Get the byte array from the memory stream
            byte[] data = ms.ToArray();

            AuthenticationServiceClient auth = new AuthenticationServiceClient();
            string authToken;
            try
            {
                authToken = auth.ValidateUser(userName, password);
            }
            catch (Exception)
            {
                return new SaveResult(false,
                    "Could not login to OSBLE. " + 
                    "You may need to reenter your user name and password.");
            }
            auth.Close();

            // Now we get the service client
            OSBLEServices.OsbleServiceClient osc = new OSBLEServices.OsbleServiceClient();
            int retVal = osc.UploadCourseGradebook(courseID, data, authToken);
            osc.Close();

            // The return value indicates the number of items in the zip that 
            // FAILED to upload if >0 or generic upload failure if == -1. A 
            // return value of 0 indicates success.
            if (retVal >= sheetCount || -1 == retVal)
            {
                return new SaveResult(
                    false,
                    string.Format(
                        "Save attempt at {0} failed because the data failed to upload " + 
                        "properly. This might occur because of a lapse in Internet " + 
                        "connectivity, so it is recommended that you try again shortly. If " + 
                        "the problem persists, please contact OSBLE support.", DateTime.Now));
            }
            else if (retVal > 0)
            {
                // This means that some sheets failed but some succeeded. We'll still 
                // issue an error message in this case.
                return new SaveResult(
                    false,
                    string.Format(
                        "Save attempt at {0} only uploaded some of the worksheets in the " + 
                        "workbook. It is recommended that you try again and/or check the " + 
                        "gradebook status online at OSBLE.org.", DateTime.Now));
            }            

            return new SaveResult(true, null);
        }

        private static string WorksheetToCSVString(Worksheet ws)
        {
            Range used = ws.UsedRange;

            // Determine the row and column count
            int rowCount = used.Rows.Count;
            int colCount = used.Columns.Count;

            // Let's arbitrarily say we need at least a 2x2
            if (rowCount < 2 || colCount < 2)
            {
                return null;
            }

            StringBuilder sb = new StringBuilder();
            for (int x = 1; x <= rowCount; x++)
            {
                for (int y = 1; y <= colCount; y++)
                {
                    if (y == colCount)
                    {
                        sb.AppendLine(used.Cells[x, y].Text);
                    }
                    else
                    {
                        sb.Append(used.Cells[x, y].Text + ",");
                    }
                }
            }

            return sb.ToString();
        }
    }
}
