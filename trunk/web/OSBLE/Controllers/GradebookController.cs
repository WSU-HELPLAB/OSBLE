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
using OSBLE.Utility;
using OSBLE.Resources;
using OSBLE.Models.Courses;
using OSBLE.Attributes;
using OSBLE.Resources.CSVReader;
using System.Threading;


namespace OSBLE.Controllers
{
    public class GradebookController : OSBLEController
    {
        public ActionResult Index(string gradebookName = null)
        {
            //Get the GradebookFilePath for current course
            GradebookFilePath gfp = new Models.FileSystem.FileSystem().Course(ActiveCourseUser.AbstractCourseID).Gradebook();

            //get last upload time
            DirectoryInfo directoryInfo = new DirectoryInfo(gfp.GetPath());
            DateTime lastUpload = directoryInfo.LastWriteTime;

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

            //If gradebook exists, set up certain viewbags
            ViewBag.GradeBookExists = gradeBookExists;
            if (gradeBookExists)
            {
                SetUpViewBagForGradebook(gradebookName);
                ViewBag.SelectedTab = gradebookName;
                ViewBag.TabNames = TabNames;
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
                //If user is an instructor and there is currnetly no gradebook, then change upload message
                ViewBag.LastUploadMessage = "Upload Gradebook File";

                //Generate additional upload fail messages. 

            }

            return View();
        }

        [HttpGet]
        public ActionResult GradebookHelp()
        {
            return View();
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
            GradebookFilePath gfp = new Models.FileSystem.FileSystem().Course(ActiveCourseUser.AbstractCourseID).Gradebook();

            //delete old items in gradebook
            int filesInGradebookFolder = gfp.AllFiles().Count;
            int filesDeleted = 0;
            TimeSpan interval = new TimeSpan(0, 0, 0, 0, 50);
            TimeSpan totalTime = new TimeSpan();
            TimeSpan maxTime = new TimeSpan(0, 0, 0, 6, 0); //4second max wait before giving up

            while(filesInGradebookFolder != filesDeleted)
            {
                filesDeleted += gfp.AllFiles().Delete();
                if (filesInGradebookFolder != filesDeleted)
                {
                    Thread.Sleep(interval);
                    totalTime += interval;
                }
                if (totalTime > maxTime)
                {
                    throw new Exception("Failed to delete all gradebook files, try again later"); ;
                }
            }
            

            int filesFailedToLoadCount = 0; //integer used for error message

            //Add new item(s) based on file extension.
            if (file == null)
            {
                //No file. Meaning 1 file failed to load.
                filesFailedToLoadCount++;
            }
            else if (Path.GetExtension(file.FileName) == ".zip")
            {
                //We have a zip file. To get the individual files out and onto the filesystem, we must first save the zip onto the filesystem.
                gfp.AddFile(file.FileName, file.InputStream);
                
                //Grab the ZipFile that was just uploaded
                ZipFile zip = ZipFile.Read(Path.Combine(gfp.GetPath(), file.FileName));

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
                        zip[i].FileName = Path.GetFileName(zip[i].FileName);
                        zip[i].Extract(gfp.GetPath());
                    }
                }
                
                //Close the ZipFile, and remove it
                zip.Dispose();
                gfp.File(file.FileName).Delete();
            }
            else if (Path.GetExtension(file.FileName) == ".csv")
            {
                //we have a .csv. Simply add it into the Gradebook directory
                gfp.AddFile(Path.GetFileName(file.FileName), file.InputStream);
            }
            else
            {
                //file wasnt csv or zip. Meaning 1 file failed to load.
                filesFailedToLoadCount++;
            }


            //Generate error message.
            if(filesFailedToLoadCount > 0)
            {
                FileCache Cache = FileCacheHelper.GetCacheInstance(OsbleAuthentication.CurrentUser);
                if(filesFailedToLoadCount == 1)
                {
                    Cache["UploadErrorMessage"] = filesFailedToLoadCount.ToString() + " file during upload was not of .csv file type, upload may have failed.";
                }
                else if(filesFailedToLoadCount > 1)
                {
                    Cache["UploadErrorMessage"] = filesFailedToLoadCount.ToString() + " files during upload were not of .csv file type, upload may have failed.";
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
            GradebookFilePath gfp = gfp = new Models.FileSystem.FileSystem().Course(ActiveCourseUser.AbstractCourseID).Gradebook();
            FileCollection gradebook = gfp.File(gradebookName + ".csv"); ;

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
                ViewBag.NameColumnIndex = Constants.StudentNameColumnIndex; //Used to 
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
                for(int i = 0; i < studentTable.Count; i++)
                {
                    for (int j = 0; j < studentTable[i].Count; j++)
                    {
                        if (studentTable[i][j].Length > 0 && studentTable[i][j][0] == '!')
                        {
                            columnsToRemove.Add(j);
                        }
                    }
                }

                //removing columns that were marked with a '!'.  
                //Removing them in from highest index to lowest index so that indicies are not messed up upon first removal
                columnsToRemove.Sort();
                for (int i = columnsToRemove.Count() - 1 ; i >= 0; i--)
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
    }
}
