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


namespace OSBLE.Controllers
{
    public class GradebookController : OSBLEController
    {
        
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
            gfp.AllFiles().Delete();

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
        /// the column matching the string in  <see cref="Constants.StudentIDColumnName"/>.
        /// </summary>
        /// <param name="gradebookName"></param>
        public void SetUpViewBagForGradebook(string gradebookName)
        {

            //Get the GradebookFilePath for current course
            GradebookFilePath gfp = new Models.FileSystem.FileSystem().Course(ActiveCourseUser.AbstractCourseID).Gradebook();

            //Get the gradebook related to gradebookName
            FileCollection gradebook = gfp.File(gradebookName + ".csv");

            //Getting the filePath, which is the filename in the file collction
            string filePath = gradebook.FirstOrDefault();

            //Open the file as a FileStream, then reading the file into a List of List of strings called table using CSVReader.
            FileStream stream = new FileStream(filePath, FileMode.Open);
            List<List<string>> table = new List<List<string>>();
            CSVReader csvReader = new CSVReader(stream);
            table = csvReader.Parse();

            //If the user is NOT an instructor or TA, then only display them rows that match their UserProfileID.
            if (ActiveCourseUser.AbstractRoleID != (int)CourseRole.CourseRoles.TA && ActiveCourseUser.AbstractRoleID != (int)CourseRole.CourseRoles.Instructor)
            {
                table = ParseStudentTable(table);
            }
            else
            {
                //for instructors, we want to know which column has the student names (used for searching in vieW)
                if (table.Count > 0)
                {
                    int nameColumn = -1;
                    for (int i = 0; i < table[0].Count; i++)
                    {
                        if (table[0][i] == Constants.StudentNameColumnName || table[0][i] == '#' + Constants.StudentNameColumnName)
                        {
                            nameColumn = i;
                            break;
                        }
                    }
                    if (nameColumn >= 0)
                    {
                        ViewBag.NameColumnIndex = nameColumn;
                    }
                         
                }
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
            if (gradebookTable.Count > 0)
            {

                int IdColumn = -1;

                //Find which column has Student ID's \
                //TODO: fix this, as it might be not exclusive to the first row anymore
                for (int i = 0; i < gradebookTable.Count; i++)
                {
                    for (int j = 0; j < gradebookTable[i].Count; j++)
                    {
                        if (gradebookTable[i][j] == Constants.StudentIDColumnName)
                        {
                            IdColumn = j;
                            break;
                        }
                    }
                }

                //find which rows should be displayed
                for (int i = 0; i < gradebookTable.Count; i++)
                {
                    //Add student's grade row, if there was a column for IDs
                    if (IdColumn >= 0 && gradebookTable[i][IdColumn] == ActiveCourseUser.UserProfile.Identification)
                    {
                        studentTable.Add(gradebookTable[i].ToList());
                    }

                    //Add global rows (denoted by a leading '#').
                    else if (gradebookTable[i][0].Length > 0 && gradebookTable[i][0][0] == '#')
                    {
                        studentTable.Add(gradebookTable[i].ToList());
                    }
                }

                //Iterating over studentTable to find columns that should not be displayed (denoted by leading '#')
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

        public ActionResult Index(string gradebookName = null)
        {
            //Get the GradebookFilePath for current course
            GradebookFilePath gfp = new Models.FileSystem.FileSystem().Course(ActiveCourseUser.AbstractCourseID).Gradebook();

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

            DirectoryInfo directoryInfo = new DirectoryInfo(gfp.GetPath());
            DateTime lastUpload = directoryInfo.LastWriteTime;

            if (gradeBookExists)
            {
                ViewBag.LastUploadMessage = "Last updated " + lastUpload.ToShortDateString().ToString() + " " + lastUpload.ToShortTimeString().ToString();
            }
            else if(ActiveCourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.Instructor || ActiveCourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.TA)
            {
                //If user is an instructor and there is currnetly no gradebook, then change upload message
                ViewBag.LastUploadMessage = "Upload Gradebook File";
            }

            return View();
        }
    }
}
