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

            Course course = db.AbstractCourses.Where(ac => ac.ID == ActiveCourseUser.AbstractCourseID).FirstOrDefault() as Course;
            ViewBag.HideMail = course.HideMail;

            return View();
        }

        [HttpGet]
        public ActionResult GradebookHelp()
        {
            return View();
        }

        public int UploadGradebookZip(byte[] zipData, GradebookFilePath gfp)
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
                filesFailedToLoadCount += ProcessGradebookChanges(gfp, newGradebook);
            }
            return filesFailedToLoadCount;
        }

        /// <summary>
        /// processes the provided gradebooks and writes them to file
        /// </summary>
        /// <param name="gfp">contains information related to the gradebook filepath</param>
        /// <param name="newGradebookDictionary">a dictionary containing a key 'filename.extension' and a list of strings representing a CSV row</param>
        /// <returns>returns an int indicating success or failure: 0 success, any positive integer will indicate a failure</returns>
        private int ProcessGradebookChanges(GradebookFilePath gfp, Dictionary<string, List<string>> newGradebookDictionary)
        {
            Dictionary<string, List<string>> mergedGradebooks = new Dictionary<string, List<string>>(); //to store all gradebooks for the course

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
                        return 1; //exit processing and add 1 to the filesFailedToLoadCount                        
                    }

                    //check if the indices of the global rows match
                    foreach (int index in newGradebookGlobalRows)
                        if (!oldGradebookGlobalRows.Contains(index)) //if each index is not found exit
                            return 1; //exit processing and add 1 to the filesFailedToLoadCount

                    //now split the rows to columns to check if they match. we know the number of global rows and index numbers match.
                    foreach (int index in newGradebookGlobalRows)
                    {
                        List<string> newGradebookColumns = newGradebook[index].Split(',').ToList();
                        List<string> oldGradebookColumns = oldGradebook[index].Split(',').ToList();

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
                                        newGradebookColumns.RemoveAt(i - 1);
                                    else
                                        return 1; //error: exit processing and add 1 to the filesFailedToLoadCount 

                                    if (newGradebookColumns.Count() == oldGradebookColumns.Count())
                                        break;
                                }
                            }
                            else
                            {
                                for (int i = oldGradebookColumns.Count(); i > oldGradebookColumns.Count() - rowDiffABS; i--)
                                {
                                    if (oldGradebookColumns[i - 1] == "")
                                        oldGradebookColumns.RemoveAt(i - 1);
                                    else
                                        return 1; //error: exit processing and add 1 to the filesFailedToLoadCount 

                                    if (newGradebookColumns.Count() == oldGradebookColumns.Count())
                                        break;
                                }
                            }
                        }

                        if (newGradebookColumns.Count() == oldGradebookColumns.Count())
                        {
                            for (int i = 0; i < newGradebookColumns.Count(); i++)
                            {
                                if (newGradebookColumns[i] != oldGradebookColumns[i])
                                    return 1; //one of the column cells doesn't match! exit and increment filesFailedToLoadCount
                            }
                        }
                        else
                        {
                            return 1; //there are a different number of columns! exit and increment filesFailedToLoadCount
                        }
                    }

                    //if we've made it this far, we have the same number of global rows and the cell contents match
                    List<string> mergedGradebook = new List<string>(oldGradebook); //created so we can merge changes while iterating the old gradebook

                    //check permission of TA
                    bool isTAUploading = false;
                    List<string> permittedSections = new List<string>();
                    int indexOfSection = -1;

                    //If the user is a TA...
                    if (ActiveCourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.TA)
                    {
                        permittedSections = GetPermittedSections(); //Get all permitted sections the TA is allowed to edit. 
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
                            if (!IsGlobalRow(newRow) && !IsGlobalRow(oldRow) && newRow.Split(',').ToList().First() == oldRow.Split(',').ToList().First()) //we found a matching row
                            {
                                if (isTAUploading)
                                {
                                    List<string> columns = newRow.Split(',').ToList(); // we need to get the index of the column with section values in it.
                                    int section;
                                    bool idParsed = Int32.TryParse(columns[indexOfSection], out section);

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
                                else
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
                            if (isTAUploading)
                            {
                                List<string> columns = newRow.Split(',').ToList(); // we need to get the index of the column with section values in it.
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
                            else //Instructor, just add it
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
            if (ActiveCourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.Instructor) //only instructors can add new gradebooks
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
            return WriteGradebooksToFile(gfp, mergedGradebooks);
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
                List<string> columns = row.Split(',').ToList(); // we need to get the index of the column with section values in it.                         

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
        private int WriteGradebooksToFile(GradebookFilePath gfp, Dictionary<string, List<string>> mergedGradebooks)
        {
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
                    //remove old gradebooks and rename temp gradebooks
                    foreach (string gradebook in gradebookNames)
                    {
                        gfp.DeleteFile(directory + "\\" + gradebook);
                        gfp.RenameFile("temp_" + gradebook, gradebook);
                    }
                }
                catch (Exception exception)
                {
                    //remove temp files
                    CleanTemporaryFiles(gfp);
                    return 1; //error! exit and increment filesFailedToLoadCount
                }
                return 0; //success!
            }
            return 1; //error! exit and increment filesFailedToLoadCount
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
                gradebook.Add(row.Split(',').ToList());
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
                List<string> rowColumns = row.Split(',').ToList();
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
        private bool IsGlobalRow(string gradebookRow)
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
        public List<string> GetPermittedSections()
        {
            //Grab the section the user is responsible for...
            int courseSection = ActiveCourseUser.Section;

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
                string multisection = ActiveCourseUser.MultiSection;

                //Make it into a list that is separated by a comma.
                List<string> multisectionList = multisection.Split(',').ToList();

                //Remove the last comma at the end of the list...
                multisectionList.RemoveAt(multisectionList.Count - 1);

                //Add the section into a list as an integer.
                foreach (string s in multisectionList)
                {
                    currentTASections.Add(s);
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
                            //List<string> gradebook = new List<string>(System.IO.File.ReadAllLines(s));
                            //zf.AddEntry(s.Split('\\').ToList().Last(), System.Text.Encoding.UTF8.GetBytes(String.Join("\n", gradebook)));
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
                        List<string> columns = row.Split(',').ToList(); // we need to get the index of the column with section values in it.                         

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
                        List<string> columns = row.Split(',').ToList();
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

            if (gradebookTable.Count > 0)
            {
                int SectionColumn = Constants.StudentSectionColumnIndex;

                //find which rows should be displayed
                for (int i = 0; i < gradebookTable.Count; i++)
                {

                    int section = 0;

                    bool sectionParse = Int32.TryParse(gradebookTable[i][SectionColumn], out section);

                    if (sectionParse && currentTASections.Contains(section.ToString()))
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

    }
}
