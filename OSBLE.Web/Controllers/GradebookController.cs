using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using Ionic.Zip;
using OSBLE.Models.FileSystem;
using OSBLE.Models;
using System.Runtime.Caching;
using OSBLE.Resources;
using OSBLE.Models.Courses;
using OSBLE.Attributes;
using OSBLE.Resources.CSVReader;
using System.Threading;
using OSBLE.Utility;
using FileCacheHelper = OSBLEPlus.Logic.Utility.FileCacheHelper;
using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;

namespace OSBLE.Controllers
{
#if !DEBUG
    [RequireHttps]
#endif

    public class GradebookController : OSBLEController
    {
        //Store all Tab Names that are located in the gradebook directory
        public static List<string> TabNames;

        //Store the gradebook file path for the gradebook directory 
        private static GradebookFilePath gfp;

        //Mutator and Accessor for all TabNames 
        public static List<string> GetSetTabNames
        {
            get
            {
                return TabNames;
            }
            set
            {
                TabNames = value;
            }
        }

        //Mutator and Accessor for GradebookFilePath
        public static GradebookFilePath GetSetGradebookFilePath
        {
            get
            {
                return gfp;
            }
            set
            {
                gfp = value;
            }
        }

        public ActionResult Index(string gradebookName = null)
        {
            //Get the GradebookFilePath for current course
            GradebookFilePath gfp = Models.FileSystem.Directories.GetGradebook(ActiveCourseUser.AbstractCourseID);

            //Set the mutator for the gradebook file path to this current gradebook directory. 
            GradebookController.GetSetGradebookFilePath = gfp;

            //Get last upload time
            DirectoryInfo directoryInfo = new DirectoryInfo(gfp.GetPath());
            DateTime lastUpload = directoryInfo.LastWriteTime.UTCToCourse(ActiveCourseUser.AbstractCourseID);

            bool deleteGradebookAbility = false;

            int userRole = ActiveCourseUser.AbstractRoleID;

            if (userRole == (int)CourseRole.CourseRoles.Instructor)
            {
                deleteGradebookAbility = true;
            }

            ViewBag.deleteGradebookAbility = deleteGradebookAbility;

            //Generating list of Gradebook tabs
            List<string> TabNames = new List<string>();
            foreach (string temp in gfp.AllFiles())
            {
                TabNames.Add(Path.GetFileNameWithoutExtension(temp));
            }

            //Selecting which gradebook will be loaded. If gradebookName is null, then select the first tab
            bool gradeBookExists = true;
            if (gradebookName == null)
            {
                if (TabNames.Count > 0)
                {
                    gradebookName = TabNames[0];
                }
                else
                {
                    gradeBookExists = false;
                }
            }

            //Set the mutator for TabNames to this instance of TabNames. 
            GradebookController.GetSetTabNames = TabNames;

            //If gradebook exists, set up certain viewbags
            ViewBag.GradeBookExists = gradeBookExists;
            if (gradeBookExists)
            {
                try
                {
                    SetUpViewBagForGradebook(gradebookName);
                    ViewBag.SelectedTab = gradebookName;
                    ViewBag.TabNames = TabNames;
                }
                catch (Exception)
                {
                    gradeBookExists = false;
                    ViewBag.TabNames = new List<string>();
                    ViewBag.SelectedTab = "";
                    ViewBag.TableData = new List<List<string>>();
                    ViewBag.GradeBookExists = false;
                }
            }

            //Setup viewbags based on usertype
            if (ActiveCourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.Instructor || ActiveCourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.TA)
            {
                //Setting instructor/Ta specific viewbags
                ViewBag.CanUpload = true;

                //Grabbing error message then wiping it.
                if (Cache["UploadErrorMessage"] != null)
                {
                    ViewBag.UploadErrorMsg = Cache["UploadErrorMessage"];
                    Cache["UploadErrorMessage"] = "";
                }
            }
            else
            {
                //Setting student specific ViewBags
                ViewBag.CanUpload = false;
            }

            if (gradeBookExists)
            {
                ViewBag.LastUploadMessage = "Last updated " + lastUpload.ToShortDateString().ToString() + " " + lastUpload.ToShortTimeString().ToString();
            }
            else if (ActiveCourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.Instructor || ActiveCourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.TA)
            {
                //If user is an instructor and there is currently no gradebook, then change upload message
                ViewBag.LastUploadMessage = "Upload Gradebook File";
                //Generate additional upload fail messages. 
            }

            ViewBag.HideMail = OSBLE.Utility.DBHelper.GetAbstractCourseHideMailValue(ActiveCourseUser.AbstractCourseID); 

            return View();
        }

        [HttpGet]
        public ActionResult GradebookHelp()
        {
            return View();
        }

        public int UploadGradebookZip(byte[] zipData, GradebookFilePath gfp, CourseUser uploadingCourseUser = null)
        {
            //Declare a variable that will keep track of how many files failed to load. 
            int filesFailedToLoadCount = 0;

            //Create a memory stream for compressed files in the Zip. 
            MemoryStream ms = new MemoryStream(zipData);
            ms.Position = 0;

            //If there are no files in the directory, put the files into the directory straight away. 
            if (gfp.AllFiles().Count() == 0)
            {
                using (ZipFile zip = ZipFile.Read(ms))
                {
                    //for each entry in zip, rename it to its FileName  and then extract it.(We rename it because zip files are named ZipFolder\Filename, 
                    //and this makes the file get added into a new folder named Zipfolder)
                    for (int i = 0; i < zip.Count; i++)
                    {
                        if (Path.GetExtension(zip[i].FileName) != ".csv")
                        {
                            filesFailedToLoadCount++;
                        }
                        else
                        {
                            //Add those extracted files from the zip right into the directory for the gradebook. 
                            zip[i].FileName = Path.GetFileName(zip[i].FileName);
                            zip[i].Extract(gfp.GetPath());
                        }
                    }
                }
            }
            else
            {
                //used to load the gradebooks in the zip file
                Dictionary<string, List<string>> newGradebook = new Dictionary<string, List<string>>();

                using (ZipFile zip = ZipFile.Read(ms))
                {
                    //for each entry in zip, rename it to its FileName  and then extract it.(We rename it because zip files are named ZipFolder\Filename, 
                    //and this makes the file get added into a new folder named Zipfolder)
                    for (int i = 0; i < zip.Count; i++)
                    {
                        if (Path.GetExtension(zip[i].FileName) != ".csv")
                        {
                            filesFailedToLoadCount++;
                        }
                        else
                        {
                            //get gradebook from zip without saving to disk
                            MemoryStream tempMemoryStream = new MemoryStream();
                            zip[i].Extract(tempMemoryStream);
                            tempMemoryStream.Position = 0;
                            StreamReader sr = new StreamReader(tempMemoryStream);
                            string temp = sr.ReadToEnd().Replace("\r", String.Empty);
                            List<string> newGradebookTab = temp.Split('\n').ToList();
                            newGradebook.Add(zip[i].FileName.Split('\\').Last(), newGradebookTab);
                        }
                    }
                }

                //process gradebooks
                filesFailedToLoadCount += ProcessGradebookChanges(gfp, newGradebook, uploadingCourseUser);
            }
            return filesFailedToLoadCount;
        }

        /// <summary>
        /// processes the provided gradebooks and writes them to file
        /// </summary>
        /// <param name="gfp">contains information related to the gradebook filepath</param>
        /// <param name="newGradebookDictionary">a dictionary containing a key 'filename.extension' and a list of strings representing a CSV row</param>
        /// <returns>returns an int indicating success or failure: 0 success, any positive integer will indicate a failure</returns>
        private int ProcessGradebookChanges(GradebookFilePath gfp, Dictionary<string, List<string>> newGradebookDictionary, CourseUser uploadingCourseUser = null)
        {
            Dictionary<string, List<string>> mergedGradebooks = new Dictionary<string, List<string>>(); //to store all gradebooks for the course
            //handle case where user is uploading from the Excel plugin
            int userRole = ActiveCourseUser == null ? uploadingCourseUser.AbstractRoleID : ActiveCourseUser.AbstractRoleID;
            int userProfileId = ActiveCourseUser == null ? uploadingCourseUser.UserProfileID : ActiveCourseUser.UserProfileID;

            //first process gradebooks that already exist
            foreach (var existingGradebook in gfp.AllFiles())
            {
                string fileName = Path.GetFileName(existingGradebook);
                if (newGradebookDictionary.ContainsKey(fileName)) //there's a matching gradebook, process changes
                {
                    List<string> oldGradebook = new List<string>(System.IO.File.ReadAllLines(existingGradebook));
                    List<string> newGradebook = new List<string>(newGradebookDictionary[fileName]);
                    List<int> newGradebookGlobalRows = new List<int>();
                    List<int> oldGradebookGlobalRows = new List<int>();

                    //now compare the gradebooks.

                    //same number of global rows?
                    foreach (string row in newGradebook)
                        if (IsGlobalRow(row))
                            newGradebookGlobalRows.Add(newGradebook.IndexOf(row));

                    foreach (string row in oldGradebook)
                        if (IsGlobalRow(row))
                            oldGradebookGlobalRows.Add(oldGradebook.IndexOf(row));

                    if (newGradebookGlobalRows.Count() != oldGradebookGlobalRows.Count()) //ERROR: gradebooks have a different number of global rows!
                    {
                        throw new Exception("GlobalRow Column Mismatch (newColumn count does not match old column count): newValue:" + String.Join(",", newGradebookGlobalRows) + 
                                                        ", oldValue: " + String.Join(",", oldGradebookGlobalRows) +
                                                        ", newCount: " + newGradebookGlobalRows.Count() + 
                                                        ", oldCount: " + oldGradebookGlobalRows.Count() + 
                                                        ", gradebook: " + existingGradebook);
                        return 1; //exit processing and add 1 to the filesFailedToLoadCount                        
                    }

                    //check if the indices of the global rows match
                    foreach (int index in newGradebookGlobalRows)
                        if (!oldGradebookGlobalRows.Contains(index)) //if each index is not found exit
                        {
                            throw new Exception("GlobalRow Column Mismatch (oldGradebookGlobalRows does not contain this index): newIndex:" +
                                                        index + ", gradebook: " + existingGradebook);
                            return 1; //exit processing and add 1 to the filesFailedToLoadCount
                        }
                            

                    //now split the rows to columns to check if they match. we know the number of global rows and index numbers match.
                    foreach (int index in newGradebookGlobalRows)
                    {   
                        //need to split but account for columns with a comma in them
                        List<string> newGradebookColumns = Regex.Split(newGradebook[index], ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)").ToList();
                        List<string> oldGradebookColumns = Regex.Split(oldGradebook[index], ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)").ToList();

                        if (newGradebookColumns.Count() != oldGradebookColumns.Count()) //column count doesn't match! check if it's just empty columns
                        {
                            //check if the longer row consists of blank spaces, if so we can adjust and continue
                            int rowDifference = newGradebookColumns.Count() - oldGradebookColumns.Count();
                            int rowDiffABS = Math.Abs(rowDifference);
                            bool newGradebookHasExtraColumns = false;

                            if (rowDifference > 0)
                                newGradebookHasExtraColumns = true;

                            if (newGradebookHasExtraColumns)
                            {
                                for (int i = newGradebookColumns.Count(); i > newGradebookColumns.Count() - rowDiffABS; i--)
                                {
                                    if (newGradebookColumns[i - 1] == "")
                                    {
                                        newGradebookColumns.RemoveAt(i - 1);
                                    }                                        
                                    else
                                    {
                                        throw new Exception("GlobalRow Column Mismatch (column counts do not match): newCount:" +
                                                        newGradebookColumns.Count() + ", oldCount: " + oldGradebookColumns.Count() + 
                                                        ", column: " + i + ", gradebook: " + existingGradebook);
                                        return 1; //error: exit processing and add 1 to the filesFailedToLoadCount 
                                    }                                        

                                    if (newGradebookColumns.Count() == oldGradebookColumns.Count())
                                        break;
                                }
                            }
                            else
                            {
                                for (int i = oldGradebookColumns.Count(); i > oldGradebookColumns.Count() - rowDiffABS; i--)
                                {
                                    if (oldGradebookColumns[i - 1] == "")
                                    {
                                        oldGradebookColumns.RemoveAt(i - 1);
                                    }                                        
                                    else
                                    {
                                        throw new Exception("GlobalRow Column Mismatch (column counts do not match): newCount:" +
                                                        newGradebookColumns.Count() + ", oldCount: " + oldGradebookColumns.Count() +
                                                        ", column: " + i + ", gradebook: " + existingGradebook);
                                        return 1; //error: exit processing and add 1 to the filesFailedToLoadCount 
                                    }                                        

                                    if (newGradebookColumns.Count() == oldGradebookColumns.Count())
                                        break;
                                }
                            }
                        }

                        if (newGradebookColumns.Count() == oldGradebookColumns.Count())
                        {
                            for (int i = 0; i < newGradebookColumns.Count(); i++)
                            {
                                if (newGradebookColumns[i].Trim() != oldGradebookColumns[i].Trim())
                                {
                                    //handle random case where a # which is hidden on the main page is in the csv
                                    if ((newGradebookColumns[i].Trim() == "" && oldGradebookColumns[i].Trim() == "#") 
                                        || "#" + newGradebookColumns[i].Trim() == oldGradebookColumns[i].Trim()
                                        || newGradebookColumns[i].Trim() + "#" == oldGradebookColumns[i].Trim())
                                    {
                                        continue;
                                    }

                                    //handle case where a ! which is hidden on the main page is in the csv (hidden rows students)
                                    if ((newGradebookColumns[i].Trim() == "" && oldGradebookColumns[i].Trim() == "!")
                                        || "!" + newGradebookColumns[i].Trim() == oldGradebookColumns[i].Trim()
                                        || newGradebookColumns[i].Trim() + "!" == oldGradebookColumns[i].Trim())
                                    {
                                        continue;
                                    }

                                    //TODO: need to handle this case somehow as it will cause the update to fail due to a different number of columns being submitted
                                    //handle case where a !! which is hidden on the main page is in the csv (hidden rows for everyone)
                                    //if ((newGradebookColumns[i].Trim() == "" && oldGradebookColumns[i].Trim() == "!!")
                                    //    || "!!" + newGradebookColumns[i].Trim() == oldGradebookColumns[i].Trim()
                                    //    || newGradebookColumns[i].Trim() + "!!" == oldGradebookColumns[i].Trim())
                                    //{
                                    //    continue;
                                    //}

                                    throw new Exception("GlobalRow Column Mismatch (newColumn does not match old column): newValue:" +
                                                        newGradebookColumns[i].Trim() + ", oldValue: " + oldGradebookColumns[i].Trim() +
                                                        ", column: " + i + ", gradebook: " + existingGradebook);
                                    return 1; //one of the column cells doesn't match! exit and increment filesFailedToLoadCount
                                }                                    
                            }
                        }
                        else
                        {
                            throw new Exception("GlobalRow Column Mismatch (newColumn count does not match old column count): newValue:" +
                                                        newGradebook[index] + ", oldValue: " + oldGradebook[index] +
                                                        ", row: " + index + ", gradebook: " + existingGradebook);
                            return 1; //there are a different number of columns! exit and increment filesFailedToLoadCount
                        }
                    }

                    //if we've made it this far, we have the same number of global rows and the cell contents match
                    List<string> mergedGradebook = new List<string>(oldGradebook); //created so we can merge changes while iterating the old gradebook

                    //check permission of TA
                    bool isTAUploading = false;
                    List<string> permittedSections = new List<string>();
                    int indexOfSection = -1;
                    bool HasMultipleSections = DBHelper.GetCourseSections(ActiveCourseUser == null ? uploadingCourseUser.AbstractCourseID : ActiveCourseUser.AbstractCourseID).Count() > 1 ? true : false;
                                        
                    //If the user is a TA...
                    if (userRole == (int)CourseRole.CourseRoles.TA)
                    {
                        permittedSections = GetPermittedSections(uploadingCourseUser); //Get all permitted sections the TA is allowed to edit. 
                        isTAUploading = true; //Set the isTAUploading 

                        //get section index
                        foreach (int index in oldGradebookGlobalRows)
                        {
                            int match = GetSectionIndex(oldGradebook[index]);
                            if (match > 0)
                                indexOfSection = match;
                        }
                    }

                    //now update row data
                    foreach (string newRow in newGradebook) //iterate the new book so we can update any matching rows and append any rows not foundin the old gradebook
                    {
                        bool rowMatch = false; //used to keep track of if we need to append a row not in the old gradebook
                        //does the newGradebook contain the row?
                        foreach (string oldRow in oldGradebook)
                        {
                            if (IsGlobalRow(newRow)) //we don't need to process this row any further, no need to search for global row matches here                           
                                break;

                            //see if we can match the current row ID column value
                            if (!IsGlobalRow(newRow) && !IsGlobalRow(oldRow) && Regex.Split(newRow, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)").ToList().First() == Regex.Split(oldRow, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)").ToList().First()) //we found a matching row
                            {
                                if (isTAUploading && HasMultipleSections)
                                {
                                    List<string> columns = Regex.Split(newRow, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)").ToList(); // we need to get the index of the column with section values in it.
                                    int section = -999;
                                    bool idParsed = indexOfSection > 0 ? Int32.TryParse(columns[indexOfSection], out section) : false;

                                    if (idParsed && permittedSections.Contains(section.ToString()))
                                    {   //add replace old row with new row in merged gradebook                                        
                                        mergedGradebook[oldGradebook.IndexOf(oldRow)] = newRow;
                                        rowMatch = true;
                                        break;
                                    }
                                    else //we didn't parse the section properly... but we don't want to try and match the row again
                                    {
                                        rowMatch = true;
                                        break;
                                    }
                                }
                                else //instructor or there are not multiple sections
                                {   //add replace old row with new row in merged gradebook
                                    mergedGradebook[oldGradebook.IndexOf(oldRow)] = newRow;
                                    rowMatch = true;
                                    break;
                                }
                            }
                        }
                        //if column doesn't exist and permitted, add row to merged gradebook
                        if (!rowMatch && !IsGlobalRow(newRow))
                        {
                            if (isTAUploading && HasMultipleSections)
                            {                                
                                List<string> columns = Regex.Split(newRow, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)").ToList(); // we need to get the index of the column with section values in it.
                                if (columns.Count() > indexOfSection) //we can't possibly have a section column here if the index is out of range!
                                {
                                    int section;
                                    bool idParsed = Int32.TryParse(columns[indexOfSection], out section);

                                    if (idParsed && permittedSections.Contains(section.ToString()))
                                    {
                                        mergedGradebook.Add(newRow); //add the row to the end of the gradebook
                                    }
                                }
                            }
                            else //Instructor or not multiple sections, just add it
                            {
                                mergedGradebook.Add(newRow); //add the row to the end of the gradebook
                            }
                        }
                    }

                    //cleanup!
                    //check if there are empty rows and remove them.
                    mergedGradebook = RemoveEmptyGradebookRows(mergedGradebook);
                    //check if there are empty columns and remove them.                    
                    mergedGradebook = RemoveEmptyGradebookColumns(mergedGradebook);

                    //we processed it all, add it to the mergedGradebooks!
                    mergedGradebooks.Add(fileName, mergedGradebook);
                }
                else
                {
                    //we didn't find it, this means
                    // a) The zip file did not contain all the gradebooks in the gradebook directory (partial update/merge, we're okay)
                    // b) Error: If the user has subfolders in the zip file causing a name mismatch
                    // c) unknown error, either way, don't add anything to the gradebook
                    //Regardless... nothing to do here, move along!
                }
            }
                        
            //now just add gradebooks that are in newGradebooks but not oldGradebooks
            if (userRole == (int)CourseRole.CourseRoles.Instructor) //only instructors can add new gradebooks
            {
                List<string> currentGradebookFilenames = new List<string>();
                foreach (var file in gfp.AllFiles())
                    currentGradebookFilenames.Add(file.Split('\\').ToList().Last());

                foreach (KeyValuePair<string, List<string>> entry in newGradebookDictionary)
                {
                    if (!currentGradebookFilenames.Contains(entry.Key)) //we only care about the gradeboks that were not already processed                
                    {   //cleanup rows and columns then add to the gradebook
                        List<string> cleanGradebook = RemoveEmptyGradebookRows(entry.Value);
                        cleanGradebook = RemoveEmptyGradebookColumns(cleanGradebook);
                        mergedGradebooks.Add(entry.Key, cleanGradebook);
                    }
                }
            }

            //mergedGradebooks now contains all gradebooks (old and new merged, including appending new rows and adding gradebooks not previously in the directory) 
            return WriteGradebooksToFile(gfp, mergedGradebooks, userProfileId);
        }

        /// <summary>
        /// detects if there is a header containing the string 'section'
        /// </summary>
        /// <param name="row">a string CSV row</param>
        /// <returns>returns the column index or a -1 if it does not find a match</returns>
        private int GetSectionIndex(string row)
        {
            if (row.ToLower().Contains("section"))
            {
                List<string> columns = Regex.Split(row, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)").ToList(); // we need to get the index of the column with section values in it.                         

                foreach (string cell in columns)
                    if (cell.ToLower().Contains("section"))
                        return columns.IndexOf(cell); //we found it! return the index                
            }
            return -1; //the row does not contain a section header
        }

        /// <summary>
        /// Takes a dictionary containing one or more 'gradebooks' in list form
        /// </summary>
        /// <param name="gfp">contains information related to the gradebook filepath</param>
        /// <param name="mergedGradebooks">a dictionary containing a key 'filename.extension' and a list of strings representing a CSV row</param>
        /// <returns>returns an int indicating success or failure: 0 success, any positive integer will indicate a failure</returns>
        private int WriteGradebooksToFile(GradebookFilePath gfp, Dictionary<string, List<string>> mergedGradebooks, int userProfileId)
        {
            try //enclosing the entire thing in a try catch so we can remove the lock if it crashes for any reason.
            {
                //check for file lock
                bool fileLocked = CheckFileLock(gfp);

                if (!fileLocked)
                {
                    //create file lock
                    CreateFileLock(gfp);
                }
                else
                {
                    int retryFileLock = 20; //wait 10 seconds trying every half second
                    while (retryFileLock != 0)
                    {
                        retryFileLock--;
                        fileLocked = CheckFileLock(gfp);
                        if (!fileLocked)
                        {
                            break;
                        }
                        Thread.Sleep(500);
                    }

                    if (!fileLocked)
                    {
                        //we waited for someone else to unlock, now create file lock so we can continue
                        CreateFileLock(gfp);
                    }
                    else
                    {
                        //the file never became 'unlocked'
                        throw new Exception("Gradebook File Lock Timed out.");
                        return 1;
                    }
                }

                string directory = gfp.GetPath().ToString();
                bool allGradebooksWritten = true;
                List<string> gradebookNames = new List<string>();

                foreach (KeyValuePair<string, List<string>> entry in mergedGradebooks)
                {
                    StreamWriter CSVStreamWriter = null; //create here so we can close it if we hit an exception anywhere in this method
                    try
                    {
                        //Construct the path for the new empty CSV file
                        string newCSVFile = directory + "\\" + "temp_" + entry.Key;
                        //Create the new empty CSV file 
                        CreateEmptyCSV(newCSVFile);
                        //Create an instance of the writer for the empty CSV file. 
                        CSVStreamWriter = new StreamWriter(newCSVFile);
                        CSVStreamWriter.WriteLine(String.Join("\n", entry.Value));
                        //Close the stream writer. 
                        CSVStreamWriter.Close();
                        gradebookNames.Add(entry.Key); //keep track of new files written so we can remove and rename
                    }
                    catch (Exception)
                    {
                        if (CSVStreamWriter != null) //close if not null!
                            CSVStreamWriter.Close();
                        //remove temp files
                        CleanTemporaryFiles(gfp);
                        allGradebooksWritten = false;
                    }
                }

                if (allGradebooksWritten)
                {
                    try
                    {
                        //create backup directory if one doesn't already exist
                        Directory.CreateDirectory(directory + "Archive");
                        //remove old gradebooks and rename temp gradebooks
                        foreach (string gradebook in gradebookNames)
                        {
                            try
                            {
                                //Move the file to the course gradebook archive zip
                                //TODO: need to handle cleaning house: a maximum number/age of archives
                                if (System.IO.File.Exists(directory + "\\" + gradebook))
                                {
                                    //System.IO.File.Move(directory + "\\" + gradebook, directory + "Archive\\" + gradebook.Split('.').ToList().First() + "-userProfileId-" + userProfileId + "-date-" + DateTime.UtcNow.ToString("MM-dd-yyyy-HH-mm-ss") + ".csv");
                                    if (System.IO.File.Exists(directory + "Archive\\GradebookArchive.zip")) //archive exists, just add it
                                    {
                                        using (ZipFile zip = ZipFile.Read(directory + "Archive\\GradebookArchive.zip"))
                                        {
                                            zip.AddFile(directory + "\\" + gradebook).FileName = gradebook.Split('.').ToList().First() + "-userProfileId-" + userProfileId + "-date-" + DateTime.UtcNow.ToString("MM-dd-yyyy-HH-mm-ss") + ".csv";
                                            zip.Save();
                                        }
                                    }
                                    else //archive doesn't exist, create it.
                                    {
                                        using (ZipFile zip = new ZipFile())
                                        {
                                            zip.AddFile(directory + "\\" + gradebook).FileName = gradebook.Split('.').ToList().First() + "-userProfileId-" + userProfileId + "-date-" + DateTime.UtcNow.ToString("MM-dd-yyyy-HH-mm-ss") + ".csv";
                                            zip.Save(directory + "Archive\\GradebookArchive.zip");
                                        }
                                    }
                                    gfp.DeleteFile(directory + "\\" + gradebook); //delete the old file                               
                                }
                                gfp.RenameFile("temp_" + gradebook, gradebook); //rename the newly updated file
                            }
                            catch (Exception exception)
                            {
                                //remove temp files
                                CleanTemporaryFiles(gfp);
                                //remove file lock
                                RemoveFileLock(gfp);
                                throw exception;
                                return 1;
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        //remove temp files
                        CleanTemporaryFiles(gfp);

                        throw new Exception("Failed to archive or rename gradebook: " + String.Join(",", gradebookNames), exception);

                        //remove file lock
                        RemoveFileLock(gfp);

                        return 1; //error! exit and increment filesFailedToLoadCount
                    }
                    //remove file lock
                    RemoveFileLock(gfp);

                    return 0; //success!
                }
                throw new Exception("Did not successfully write all gradebooks: " + String.Join(",", gradebookNames));

                //remove temp files
                CleanTemporaryFiles(gfp);
                //remove file lock
                RemoveFileLock(gfp);

                return 1; //error! exit and increment filesFailedToLoadCount
            }
            catch (Exception exception) //clean up and throw exception
            {
                //remove temp files
                CleanTemporaryFiles(gfp);
                //remove file lock
                RemoveFileLock(gfp);
                throw new Exception("WriteGradebooksToFile() failsafe...", exception);
                return 1; //error! exit and increment filesFailedToLoadCount
            }            
        }

        private void RemoveFileLock(GradebookFilePath gfp)
        {
            if (System.IO.File.Exists(gfp.GetPath() + "Lock\\gb.lock"))
            {
                System.IO.File.Delete(gfp.GetPath() + "Lock\\gb.lock");
            }
        }

        private void CreateFileLock(GradebookFilePath gfp)
        {
            Directory.CreateDirectory(gfp.GetPath() + "Lock");

            TextWriter tw = new StreamWriter(gfp.GetPath() + "Lock\\gb.lock", true);            
            tw.Close();
        }

        private bool CheckFileLock(GradebookFilePath gfp)
        {
            return System.IO.File.Exists(gfp.GetPath() + "Lock\\gb.lock");
        }

        /// <summary>
        /// tries to delete temp_ files generated during the upload process
        /// </summary>
        /// <param name="gfp">contains information related to the gradebook filepath</param>
        private void CleanTemporaryFiles(GradebookFilePath gfp)
        {
            foreach (var file in gfp.AllFiles())
            {
                try
                {
                    string fileName = file.Split('\\').ToList().Last();
                    if (fileName.StartsWith("temp_"))
                    {                        
                        gfp.DeleteFile(gfp.GetPath() + "\\" + fileName);
                    }
                }
                catch (Exception exception)
                {
                    //TODO: handle exception error message
                }
            }
        }

        /// <summary>
        /// removes all 'empty' columns from the right most column until the lenght of the longest global row
        /// </summary>
        /// <param name="mergedGradebook">a gradebook represented as a list of CSV string rows</param>
        /// <returns>returns the processed list</returns>
        private List<string> RemoveEmptyGradebookColumns(List<string> mergedGradebook)
        {
            List<List<string>> gradebook = new List<List<string>>();

            //break the merged gradebook into its rows and columns
            foreach (string row in mergedGradebook)
            {
                gradebook.Add(Regex.Split(row, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)").ToList());
            }

            int longestGlobalRowLength = 0;

            //use the longest global row's non empty cell as the guide
            foreach (List<string> row in gradebook)
            {
                if (row.First().Contains("#")) //global row
                {
                    for (int i = row.Count() - 1; i >= 0; i--)
                    {
                        if (row[i] != "")
                        {
                            if (i > longestGlobalRowLength)
                            {
                                longestGlobalRowLength = i;
                            }
                            break;
                        }
                    }
                }
            }

            //we now know the longest non empty global row length
            //strip empty end row values until it matches LONGEST global row length
            List<List<string>> fixedColumnGradebook = new List<List<string>>();
            foreach (List<string> row in gradebook)
            {
                List<string> combinedRow = new List<string>(row);
                for (int i = combinedRow.Count() - 1; i > longestGlobalRowLength; i--)
                    combinedRow.RemoveAt(i);

                fixedColumnGradebook.Add(combinedRow);
            }

            List<string> processedGradebook = new List<string>();
            //now join the columns back to individual strings and return list.
            foreach (List<string> row in fixedColumnGradebook)
            {
                processedGradebook.Add(String.Join(",", row));
            }

            return processedGradebook;
        }

        /// <summary>
        /// removes any rows consisting of only empty cells
        /// </summary>
        /// <param name="mergedGradebook">a gradebook represented as a list of CSV string rows</param>
        /// <returns>returns the processed list</returns>
        private List<string> RemoveEmptyGradebookRows(List<string> mergedGradebook)
        {
            List<string> processedGradebook = new List<string>(mergedGradebook);
            foreach (string row in mergedGradebook)
            {
                List<string> rowColumns = Regex.Split(row, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)").ToList();
                bool emptyRow = true;
                foreach (string column in rowColumns)
                {
                    //if we find at least one non empty cell we can't consider it an empty row, break loop and keep looking
                    if (column != "")
                    {
                        emptyRow = false;
                        break;
                    }
                }
                if (emptyRow) //if we've iterated over the entire column and all cells are empty, remove the row
                {
                    processedGradebook.Remove(row);
                }
            }
            return processedGradebook;
        }

        /// <summary>
        /// checks if the string row is a 'global' row
        /// </summary>
        /// <param name="gradebookRow">a string CSV row</param>
        /// <returns>true if the row starts with a '#'</returns>
        private static bool IsGlobalRow(string gradebookRow)
        {
            if (gradebookRow.Length > 0 && gradebookRow[0] == '#') //# indicates a global row            
                return true;
            else
                return false;
        }

        /// <summary>
        /// Creates a CSV to store updated lines to new gradebook csv file. 
        /// </summary>
        /// <param name="filename">Passes a string value to be the name of the empty file</param>
        public static void CreateEmptyCSV(string filename)
        {
            System.IO.File.Create(filename).Dispose();
        }

        /// <summary>
        /// Get the permitted sections that the user is able to edit and upload. 
        /// </summary>
        /// <returns>a list of the sections</returns>
        public List<string> GetPermittedSections(CourseUser uploadingCourseUser = null)
        {
            CourseUser currentUser = ActiveCourseUser == null ? uploadingCourseUser : ActiveCourseUser;

            //Grab the section the user is responsible for...
            int courseSection = currentUser.Section;

            //Create and empty list of sections the TA is responsible for...
            List<string> currentTASections = new List<string>();

            // If the TA is reponsible for only one section... 
            if (courseSection >= 0)
            {
                //Add that single section to the empty list of current TA sections they are permitted to edit and upload...
                currentTASections.Add(courseSection.ToString());
            }           
            else
            {
                //Grab the sections the user is able to edit.
                string multisection = currentUser.MultiSection;

                if (multisection == "all") //add all course sections
                {
                    var allCourseSections = db.CourseUsers.Where(cu => cu.AbstractCourseID == currentUser.AbstractCourseID).Select(s => s.Section).ToList().Distinct();
                    foreach (var section in allCourseSections)
                    {
                        if (section != -1 || section != -2 )
                        {
                            currentTASections.Add(section.ToString());
                        }
                    }                    
                }
                else //parse specific sections
                {
                    //Get list of sections
                    List<string> multisectionList = multisection.Split(',').ToList();                    
                    
                    foreach (string s in multisectionList)
                    {
                        if (s != "") //don't add any empty items
                        {
                            currentTASections.Add(s);
                        }
                    }
                }
            }
            return currentTASections;
        }

        /// <summary>
        /// Delete all gradebooks and return back to the index
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [CanGradeCourse]
        public ActionResult DeleteAllGradebooks()
        {
            if (null != ActiveCourseUser && ActiveCourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.Instructor)
            {
                GradebookFilePath gfp = Models.FileSystem.Directories.GetGradebook(ActiveCourseUser.AbstractCourseID);
                gfp.AllFiles().Delete();
            }
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Delete the selected gradebook and return back to the index
        /// </summary>
        /// <returns></returns>        
        [CanGradeCourse]
        public ActionResult DeleteSingleGradebook(string gradebookName)
        {
            if (null != ActiveCourseUser && ActiveCourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.Instructor)
            {
                GradebookFilePath gfp = Models.FileSystem.Directories.GetGradebook(ActiveCourseUser.AbstractCourseID);
                try
                {
                    gfp.DeleteFile(gradebookName + ".csv");
                }
                catch (Exception)
                {
                    //TODO: handle exception, for now do nothing.
                }

            }
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Download all gradebooks from the gradebook directory
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [CanGradeCourse]
        public ActionResult DownloadGradebook()
        {
            //Gradebook file path according to who the user is...
            GradebookFilePath gfp = Directories.GetGradebook(ActiveCourseUser.AbstractCourseID);

            if (gfp.AllFiles().Count() > 0)
            {
                //Declare an instance of zipfile 
                ZipFile zf = new ZipFile();

                //Grab the name of the course and contactenate it to the gradebook.zip
                string zipName = ActiveCourseUser.AbstractCourse.Name.ToString() + "_Gradebook.zip";

                //For each string in the gradebook directory...
                foreach (string s in gfp.AllFiles())
                {
                    //If the user is a TA we need to only serve rows with their permitted sections.
                    if (ActiveCourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.TA)
                    {
                        //Get all permitted sections the TA is allowed to edit. 
                        List<string> permittedSections = GetPermittedSections();
                        List<int> permittedSectionUploadInt = new List<int>();

                        foreach (string sectionString in permittedSections)
                        {
                            int section;
                            bool idParsed = Int32.TryParse(sectionString, out section);
                            if (idParsed)
                                permittedSectionUploadInt.Add(section);
                        }

                        //build partial gradebook for TAs - contains only rows for their section
                        List<string> partialGradebook = GeneratePartialGradebook(s, permittedSectionUploadInt);

                        //TODO: discuss this, should we require TAs to have a section? for now disable downloading of the entire gradebook for this case!
                        if (String.Equals("No Sections", partialGradebook.FirstOrDefault())) //if no sections, they presumably are in charge of all student grades...
                        {
                            //Add the gradebook to the list of zipped files.
                            List<string> gradebook = new List<string>(System.IO.File.ReadAllLines(s));
                            zf.AddEntry(s.Split('\\').ToList().Last(), System.Text.Encoding.UTF8.GetBytes(String.Join("\n", gradebook)));
                        }
                        else
                        {
                            //add the partial file to the zip
                            zf.AddEntry(s.Split('\\').ToList().Last(), System.Text.Encoding.UTF8.GetBytes(String.Join("\n", partialGradebook)));
                        }
                    }
                    else //instructor... just add the gradebook to the zip
                    {
                        //Add the gradebook to the list of zipped files.
                        List<string> gradebook = new List<string>(System.IO.File.ReadAllLines(s));
                        zf.AddEntry(s.Split('\\').ToList().Last(), System.Text.Encoding.UTF8.GetBytes(String.Join("\n", gradebook)));
                    }
                }

                //create a memory stream to save the zipfile to
                MemoryStream stream = new MemoryStream();
                zf.Save(stream);
                stream.Position = 0;

                //return the zip stream as a byte array to the user
                return File(stream.ToArray(), "application/zip", zipName);
            }
            else
            {
                //TODO: redirect to an appropriate error page
                return RedirectToAction("Index");
            }
        }
        
        /// <summary>
        /// Generates a partial gradebook (as a list of strings) which contains only rows which the user requesting the gradebook is allowed access to.
        /// </summary>
        /// <param name="filePathWithExtension">full file path including file name and extension e.g. C:\FileSystem\SomeGradebookFile.csv</param>
        /// <param name="permittedSectionUpload">list of integer values representing sections the user is permitted to access</param>
        /// <returns>Returns a list of strings representing a gradebook where each string represents a row in the csv file</returns>
        private List<string> GeneratePartialGradebook(string filePathWithExtension, List<int> permittedSectionUpload)
        {
            List<string> gradebook = new List<string>(System.IO.File.ReadAllLines(filePathWithExtension));
            List<string> partialGradebook = new List<string>();
            int sectionIndex = -1;
            bool hasSectionsHeader = false;
            bool partialGradebookCompleted = false;

            foreach (string row in gradebook) //we need to make sure the gradebook has sections
            {
                if (row.Length > 0 && row.Contains('#'))
                {
                    if (row.ToLower().Contains("section")) //global row
                    {
                        List<string> columns = Regex.Split(row, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)").ToList(); // we need to get the index of the column with section values in it.                         

                        foreach (string cell in columns)
                        {
                            if (cell.ToLower().Contains("section"))
                            {
                                sectionIndex = columns.IndexOf(cell);
                                break; //we got what we're here for, stop searching.
                            }
                        }
                        hasSectionsHeader = true;
                        break;
                    }
                }
            }

            if (hasSectionsHeader && sectionIndex >= 0) //if we found header rows and we also found the "section" header column index
            {
                foreach (string row in gradebook)
                {
                    if (row.Length > 0 && row.Contains('#')) //global row, just add it
                    {
                        partialGradebook.Add(row);
                    }
                    else
                    {
                        // we need to check the index of the column with section values in it to make sure the section matches the TA's section.
                        List<string> columns = Regex.Split(row, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)").ToList();
                        int section;
                        bool idParsed = Int32.TryParse(columns[sectionIndex], out section);

                        if (idParsed && permittedSectionUpload.Contains(section))
                        {
                            //if we were able to successfully parse the section value for the row and the user is in the proper section... add the row!
                            partialGradebook.Add(row);
                        }
                    }
                }
                partialGradebookCompleted = true;
            }

            if (partialGradebookCompleted)
            {
                return partialGradebook;
            }
            else
            {
                List<string> noSections = new List<string>();
                noSections.Add("No Sections");
                return noSections;
            }
        }

        /// <summary>
        /// Should take a string which is the entire gradebook in comma seprated with a newline at the end of each row
        /// </summary>
        /// <param name="modifiedGradeook"></param>
        /// <returns></returns>
        [HttpPost]
        [CanGradeCourse]
        public bool UpdateGradebookFromPage(string gradebookName, string modifiedGradeook)
        {
            modifiedGradeook = HttpUtility.UrlDecode(modifiedGradeook);

            //Get the GradebookFilePath for current course
            GradebookFilePath gfp = Models.FileSystem.Directories.GetGradebook(ActiveCourseUser.AbstractCourseID);

            Dictionary<string, List<string>> newGradebook = new Dictionary<string, List<string>>();
            
            newGradebook.Add(gradebookName + ".csv", modifiedGradeook.Split('\n').ToList());
            
            int filesFailedToLoadCount = 0; //integer used for error message
            filesFailedToLoadCount += ProcessGradebookChanges(gfp, newGradebook);

            //Generate error message.
            if (filesFailedToLoadCount > 0)
            {
                FileCache Cache = FileCacheHelper.GetCacheInstance(OsbleAuthentication.CurrentUser);
                if (filesFailedToLoadCount >= 1)
                {
                    Cache["UploadErrorMessage"] = filesFailedToLoadCount.ToString() + " file(s) during upload was not of .csv file type or did not match current gradebook format, upload may have failed.";
                }
                //we had an error, return false;
                return false;
            }
            //no errors, return true
            return true;
        }

        /// <summary>
        /// This function takes a .csv or .zip. It then takes the .csv or collection of .csv files (from the zip) and adds them into the Gradebook folder
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        [HttpPost]
        [CanGradeCourse]
        public ActionResult UploadGradebook(HttpPostedFileBase file)
        {
            //Get the GradebookFilePath for current course
            GradebookFilePath gfp = Models.FileSystem.Directories.GetGradebook(ActiveCourseUser.AbstractCourseID);

            //delete old items in gradebook
            int filesInGradebookFolder = gfp.AllFiles().Count;
            int filesDeleted = 0;
            TimeSpan interval = new TimeSpan(0, 0, 0, 0, 50);
            TimeSpan totalTime = new TimeSpan();
            TimeSpan maxTime = new TimeSpan(0, 0, 0, 6, 0); //4second max wait before giving up

            int filesFailedToLoadCount = 0; //integer used for error message

            //Add new item(s) based on file extension.
            if (file == null)
            {
                //No file. Meaning 1 file failed to load.
                filesFailedToLoadCount++;
            }
            else if (Path.GetExtension(file.FileName) == ".zip")
            {
                using (MemoryStream zipStream = new MemoryStream())
                {
                    file.InputStream.CopyTo(zipStream);
                    zipStream.Position = 0;
                    filesFailedToLoadCount += UploadGradebookZip(zipStream.ToArray(), gfp);
                }
            }
            else if (Path.GetExtension(file.FileName) == ".csv")
            {
                Dictionary<string, List<string>> newGradebook = new Dictionary<string, List<string>>();
                //
                newGradebook.Add(file.FileName, new StreamReader(file.InputStream).ReadToEnd().Replace("\r", String.Empty).Split('\n').ToList());

                filesFailedToLoadCount += ProcessGradebookChanges(gfp, newGradebook);
            }
            else
            {
                //file wasnt csv or zip. Meaning 1 file failed to load.
                filesFailedToLoadCount++;
            }

            //Generate error message.
            if (filesFailedToLoadCount > 0)
            {
                FileCache Cache = FileCacheHelper.GetCacheInstance(OsbleAuthentication.CurrentUser);
                if (filesFailedToLoadCount >= 1)
                {
                    Cache["UploadErrorMessage"] = filesFailedToLoadCount.ToString() + " file(s) during upload was not of .csv file type or did not match current gradebook format, upload may have failed.";
                }
            
            }
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Sets up ViewBags for the given gradebookName. The assumption made in this function is that the StudentID number is in
        /// column 0.
        /// </summary>
        /// <param name="gradebookName"></param>
        private void SetUpViewBagForGradebook(string gradebookName)
        {
            //Get the GradebookFilePath for current course, then the FileCollection for the given gradebookName
            GradebookFilePath gfp = Models.FileSystem.Directories.GetGradebook(
                ActiveCourseUser.AbstractCourseID);
            FileCollection gradebook = gfp.File(gradebookName + ".csv");

            //Getting the filePath, which is the filename in the file collction
            string filePath = gradebook.FirstOrDefault();

            //Open the file as a FileStream. For this, we want to wrap it in a try/catch block, as others might be attempting to use this stream
            //at the same time. We'll allow it attempt to open the stream for up to maxTime.
            FileStream stream = null;
            TimeSpan interval = new TimeSpan(0, 0, 0, 0, 50);
            TimeSpan totalTime = new TimeSpan();
            TimeSpan maxTime = new TimeSpan(0, 0, 0, 4, 0); //4second max wait before giving up
            while (stream == null)
            {
                try
                {
                    //Get the stream related to the current file
                    stream = new FileStream(filePath, FileMode.Open);

                }
                catch (IOException ex)
                {
                    Thread.Sleep(interval);
                    totalTime += interval;

                    //if we've waited longer than maxTime, throw the original exception
                    if (totalTime > maxTime)
                    {
                        throw ex;
                    }
                }
            }

            //reading the file into a List of List of strings using CSVReader.
            List<List<string>> table = new List<List<string>>();
            CSVReader csvReader = new CSVReader(stream);
            table = csvReader.Parse();
            stream.Close(); //close the stream to allow others to access it. 

            //If the user is NOT an instructor or TA, then only display them rows that match their UserProfileID.
            if (ActiveCourseUser.AbstractRoleID != (int)CourseRole.CourseRoles.TA && ActiveCourseUser.AbstractRoleID != (int)CourseRole.CourseRoles.Instructor)
            {
                table = ParseStudentTable(table);
            }
            else if (ActiveCourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.TA)
            {
                table = ParseTATable(table);
            }
            else
            {
                List<int> globalRows = new List<int>();
                List<int> hiddenColumns = new List<int>();
                //find which rows should be displayed as "globals"
                for (int i = 0; i < table.Count; i++)
                {
                    //Add global rows (denoted by a leading '#').
                    if (table[i][0].Length > 0 && table[i][0][0] == '#')
                    {
                        globalRows.Add(i);
                        for (int j = 0; j < table[i].Count; j++) //go through each cell in global row and check for hidden column values
                        {
                            if (table[i][j].Length > 2 && table[i][j][0] == '!' && table[i][j][1] == '!')
                            {
                                hiddenColumns.Add(j);
                            }
                        }
                    }
                }

                ViewBag.Instructor_ColumnsToHide = hiddenColumns;
                ViewBag.NameColumnIndex = Constants.StudentNameColumnIndex;
                ViewBag.SectionColumnIndex = Constants.StudentSectionColumnIndex;
                ViewBag.GlobalRows = globalRows;
            }

            ViewBag.TableData = table;
        }

        /// <summary>
        /// Takes a full gradebook table and parses the table down to rows that have a leading "#" (indicating they are "global" rows)
        /// and columns that do not have a leading "!" (indiciating they are not to be shown). Additionally, the row that corrisponds to the current user
        /// (indicated by identification number in the StudentID column) will be included.
        /// </summary>
        /// <param name="gradebookTable"></param>
        /// <returns></returns>
        private List<List<string>> ParseStudentTable(List<List<string>> gradebookTable)
        {

            List<List<string>> studentTable = new List<List<string>>();
            List<int> studentGlobalRows = new List<int>();
            if (gradebookTable.Count > 0)
            {

                int IdColumn = Constants.StudentIDColumnIndex;

                //find which rows should be displayed
                for (int i = 0; i < gradebookTable.Count; i++)
                {
                    //If its student's grade row, add it.
                    if (gradebookTable[i][IdColumn] == ActiveCourseUser.UserProfile.Identification)
                    {
                        studentTable.Add(gradebookTable[i].ToList());
                    }

                    //Add global rows (denoted by a leading '#').
                    else if (gradebookTable[i][0].Length > 0 && gradebookTable[i][0][0] == '#')
                    {
                        studentTable.Add(gradebookTable[i].ToList());
                        studentGlobalRows.Add(studentTable.Count - 1);
                    }
                }
                ViewBag.GlobalRows = studentGlobalRows;

                //Iterating over studentTable to find columns that should not be displayed (denoted by leading '!')
                List<int> columnsToRemove = new List<int>();
                for (int i = 0; i < studentTable.Count; i++)
                {
                    for (int j = 0; j < studentTable[i].Count; j++)
                    {
                        if (studentTable[i][j].Length > 0 && studentTable[i][j][0] == '!' || studentTable[i][j].Length > 0 && studentTable[i][j][studentTable[i][j].Length - 1] == '!')
                        {
                            columnsToRemove.Add(j);
                        }
                    }
                }

                //removing columns that were marked with a '!'.  
                //Removing them in from highest index to lowest index so that indicies are not messed up upon first removal
                columnsToRemove.Sort();
                for (int i = columnsToRemove.Count() - 1; i >= 0; i--)
                {
                    int currentStudentTableLength = studentTable.Count;
                    for (int j = 0; j < currentStudentTableLength; j++)
                    {
                        studentTable[j].RemoveAt(columnsToRemove[i]);
                    }
                }
            }
            return studentTable;
        }

        /// <summary>
        /// Takes a full gradebook table and parses the table down to rows that have a leading "#" (indicating they are "global" rows)
        /// and columns that do not have a leading "!" (indiciating they are not to be shown). This has special permissions when it comes to what a TA and instructor could see. 
        /// </summary>
        /// <param name="gradebookTable"></param>
        /// <returns></returns>
        private List<List<string>> ParseTATable(List<List<string>> gradebookTable)
        {
            
            //Declare all necessary empty lists.
            List<string> currentTASections = new List<string>();
            List<List<string>> TATable = new List<List<string>>();
            List<int> studentGlobalRows = new List<int>();

            //Grab all sections that are able to be editted and uploaded by the TA
            currentTASections = GetPermittedSections();
            //get sections in course
            bool HasMultipleSections = DBHelper.GetCourseSections(ActiveCourseUser.AbstractCourseID).Count() > 1 ? true : false;

            if (gradebookTable.Count > 0)
            {
                //find section index
                int SectionColumn = -1;
                for (int i = 0; i < gradebookTable.Count; i++)
                {
                    SectionColumn = GetSectionIndex(String.Join(",", gradebookTable[i].ToList()));
                    if (SectionColumn != -1)
                        break; //found a section column, exit loop!
                }

                //find which rows should be displayed
                for (int i = 0; i < gradebookTable.Count; i++)
                {

                    int section = 0;
                    bool sectionParse = SectionColumn > 0 ? Int32.TryParse(gradebookTable[i][SectionColumn], out section) : false;

                    if (!HasMultipleSections || (sectionParse && currentTASections.Contains(section.ToString())))
                    {
                        TATable.Add(gradebookTable[i].ToList());
                    }

                    //Add global rows (denoted by a leading '#').
                    else if (gradebookTable[i][0].Length > 0 && gradebookTable[i][0][0] == '#')
                    {
                        TATable.Add(gradebookTable[i].ToList());
                        studentGlobalRows.Add(TATable.Count - 1);
                    }
                }

                ViewBag.GlobalRows = studentGlobalRows;

                //Iterating over studentTable to find columns that should not be displayed (denoted by leading '!')
                List<int> columnsToRemove = new List<int>();
                for (int i = 0; i < TATable.Count; i++)
                {
                    for (int j = 0; j < TATable[i].Count; j++)
                    {
                        if (TATable[i][j].Length > 0 && TATable[i][j][0] == '!' || TATable[i][j].Length > 0 && TATable[i][j][TATable[i][j].Length - 1] == '!')
                        {
                            columnsToRemove.Add(j);
                        }
                    }
                }

                //removing columns that were marked with a '!'.  
                //Removing them in from highest index to lowest index so that indicies are not messed up upon first removal
                columnsToRemove.Sort();

                for (int i = columnsToRemove.Count() - 1; i >= 0; i--)
                {
                    int currentStudentTableLength = TATable.Count;
                    for (int j = 0; j < currentStudentTableLength; j++)
                    {
                        try
                        {
                            TATable[j].RemoveAt(columnsToRemove[i]);
                        }
                        catch (Exception e)
                        {

                        }
                    }
                }
            }
            return TATable;
        }

        /// <summary>
        /// Download all gradebooks from the gradebook directory in one Excel.xlsx file
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [CanGradeCourse]
        public ActionResult DownloadGradebookXLSX()
        {
            //Grab the name of the course and contactenate it to the gradebook name
            string workbookName = ActiveCourseUser.AbstractCourse.Name.ToString() + "_Gradebook.xlsx";

            //create the workbook to add each tab as a worksheet to
            XLWorkbook workbook = new XLWorkbook();

            //Gradebook file path according to who the user is...
            GradebookFilePath gfp = Directories.GetGradebook(ActiveCourseUser.AbstractCourseID);

            if (gfp.AllFiles().Count() > 0)
            {
                //For each string in the gradebook directory...
                foreach (string s in gfp.AllFiles())
                {
                    //If the user is a TA we need to only serve rows with their permitted sections.
                    if (ActiveCourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.TA)
                    {
                        //Get all permitted sections the TA is allowed to edit. 
                        List<string> permittedSections = GetPermittedSections();
                        List<int> permittedSectionUploadInt = new List<int>();

                        foreach (string sectionString in permittedSections)
                        {
                            int section;
                            bool idParsed = Int32.TryParse(sectionString, out section);
                            if (idParsed)
                                permittedSectionUploadInt.Add(section);
                        }

                        //build partial gradebook for TAs - contains only rows for their section
                        List<string> partialGradebook = GeneratePartialGradebook(s, permittedSectionUploadInt);

                        //TODO: discuss this, should we require TAs to have a section? for now disable downloading of the entire gradebook for this case!
                        if (String.Equals("No Sections", partialGradebook.FirstOrDefault())) //if no sections, they presumably are in charge of all student grades...
                        {
                            //add the gradebook to the xlsx as a worksheet
                            workbook = ConvertWithClosedXml(workbook, s.Split('\\').ToList().Last().Split('.').First(), ReadCsv(s));
                        }
                        else
                        {
                            //add the gradebook to the xlsx as a worksheet
                            workbook = ConvertWithClosedXml(workbook, s.Split('\\').ToList().Last().Split('.').First(), partialGradebook);
                        }
                    }
                    else //instructor... just add the gradebook to the zip
                    {
                        //add the gradebook to the xlsx as a worksheet
                        workbook = ConvertWithClosedXml(workbook, s.Split('\\').ToList().Last().Split('.').First(), ReadCsv(s));
                    }
                }

                //save workbook
                //create a memory stream to save the workbook to
                MemoryStream stream = new MemoryStream();                
                workbook.SaveAs(stream);
                stream.Position = 0;

                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", workbookName);
            }
            else
            {
                //TODO: redirect to an appropriate error page
                return RedirectToAction("Index");
            }
        }

        private List<string> ReadCsv(string fileName)
        {
            List<string> lines = System.IO.File.ReadAllLines(fileName).ToList(); 
            return lines;
        }

        private static XLWorkbook ConvertWithClosedXml(XLWorkbook workbook, string worksheetName, List<string> csvLines)
        {
            if (csvLines == null || csvLines.Count() == 0)
            {
                return (workbook);
            }

            int rowCount = 0;
            int colCount = 0;
            
            using (var worksheet = workbook.Worksheets.Add(worksheetName))
            {
                rowCount = 1;
                foreach (string line in csvLines)
                {
                    bool globalRow = IsGlobalRow(line);
                    colCount = 1;
                    //split line to columns list
                    List<string> linesColumnSplit = Regex.Split(line, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)").ToList();
                    foreach (string col in linesColumnSplit)
                    {
                        if (!globalRow && col.Contains("%")) //percent, strip percent and process number
                        {
                            string colNumber = col.Replace("%", "");
                            double pct = 0.0;                            
                            bool doubleParseSuccess = Double.TryParse(colNumber, out pct); 
                            var regex = new Regex(@"^-*[0-9,\.]+$");

                            if (doubleParseSuccess && regex.IsMatch(colNumber))
                            {
                                worksheet.Cell(rowCount, colCount).Value = pct / 100; //divide by 100 because the input value was a percent
                                worksheet.Cell(rowCount, colCount).Style.NumberFormat.Format = "0.00%";    
                            }
                            else //not an integer or decimal for some reason!
                            {
                                worksheet.Cell(rowCount, colCount).Value = TypeConverter.TryConvert(col);  
                            }                            
                        }
                        else
                        {
                            if (globalRow) //TODO: need to modify this if/when the gradebook is re-adjusted to handle dynamic global rows.
                            {   //preserve the exact format of the data, we don't want to convert it or else it will prevent updating
                                worksheet.Cell(rowCount, colCount).Value = col;
                                worksheet.Cell(rowCount, colCount).SetDataType(XLCellValues.Text);
                            }
                            else
                            {
                                worksheet.Cell(rowCount, colCount).Value = TypeConverter.TryConvert(col);   
                            }
                        }                        
                        colCount++;
                    }
                    rowCount++;
                }
                //adjust column widths... e.g. to prevent 3333333333 from being converted to an exponential representation
                worksheet.Worksheet.Columns().AdjustToContents();
            }            
            return (workbook);
        }        
    }

    /// <summary>
    /// class used to convert cell values to an appropriate numeric value
    /// </summary>
    public static class TypeConverter
    {
        /// <summary>
        /// Converts the string cell values into an appropriate numeric, boolean, or date value.
        /// </summary>
        /// <param name="value">a single string value</param>
        /// <returns>a converted object of string, numeric, boolean, or datetime value</returns>
        public static object TryConvert(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return (string.Empty);
            }

            int intValue = 0;
            if (int.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture.NumberFormat, out intValue))
            {
                return (intValue);
            }

            double doubleValue = 0;
            if (double.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture.NumberFormat, out doubleValue))
            {
                return (doubleValue);
            }

            float floatValue = 0;
            if (float.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture.NumberFormat, out floatValue))
            {
                return (floatValue);
            }

            long longValue = 0;
            if (long.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture.NumberFormat, out longValue))
            {
                return (longValue);
            }

            bool boolValue = false;
            if (bool.TryParse(value, out boolValue))
            {
                return (boolValue);
            }

            DateTime dateTimeValue = DateTime.MinValue;
            if (DateTime.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out dateTimeValue))
            {
                return (dateTimeValue);
            }

            return (value);
        }
    }
}
