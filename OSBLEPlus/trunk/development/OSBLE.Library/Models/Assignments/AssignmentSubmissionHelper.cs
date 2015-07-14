using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Ionic.Zip;
using OSBLE.Models.Courses;

namespace OSBLE.Models.Assignments
{
    public class AssignmentSubmissionHelper
    {
        public static string[] GetFileExtensions(DeliverableType deliverableType)
        {
            var type = deliverableType.GetType();

            var fi = type.GetField(deliverableType.ToString());

            //we get the attributes of the selected language
            var attrs = (fi.GetCustomAttributes(typeof(FileExtensions), false) as FileExtensions[]);

            //make sure we have more than (should be exactly 1)
            if (attrs != null && (attrs.Length > 0 && attrs[0] != null))
            {
                return attrs[0].Extensions;
            }

            //throw and exception if not decorated with any attrs because it is a requirement
            throw new Exception("Languages must have be decorated with a FileExtensionAttribute");
        }

        public static List<Deliverable> UpdateDeliverables(IEnumerable<HttpPostedFileBase> files, Assignment assignment)
        {
            List<Deliverable> deliverables;
            if (assignment.Type == AssignmentTypes.CriticalReview)
            {
                deliverables = new List<Deliverable>((assignment.PreceedingAssignment).Deliverables);
            }
            else if (assignment.Type == AssignmentTypes.AnchoredDiscussion)
            {
                using (var db = new OSBLEContext())
                {
                    //TODO: need to keep deliverables if no changes have been made.
                    //need to remove old deliverables
                    assignment.Deliverables.Clear();
                    db.SaveChanges();
                    deliverables = new List<Deliverable>((assignment).Deliverables);
                    List<string> deliverableNames = new List<string>();

                    deliverables.AddRange(files.Select(file => new Deliverable
                    {
                        Assignment = assignment,
                        AssignmentID = assignment.ID,
                        DeliverableType = DeliverableType.PDF,
                        Name = Path.GetFileNameWithoutExtension(file.FileName)
                    }));

                    foreach (Deliverable d in deliverables)
                    {
                        assignment.Deliverables.Add(d);
                    }
                    db.Entry(assignment).State = System.Data.EntityState.Modified;
                    db.SaveChanges();
                }
            }
            else
            {
                deliverables = new List<Deliverable>((assignment).Deliverables);
            }
            return deliverables;
        }

        public static bool SubmitAssignments(NameValueCollection request, CourseUser user, string firstName,
            string lastName, int? id, IEnumerable<HttpPostedFileBase> files, int? authorTeamID,
            Assignment assignment, List<Deliverable> deliverables, out AssignmentTeam assignmentTeam,
            ref List<string> deliverableNames)
        {
            assignmentTeam = user.TeamMemberships
                .SelectMany(t => t.Team.UsedAsAssignmentTeam)
                .FirstOrDefault(a => a.AssignmentID == assignment.ID);

            int i = 0;

            if (deliverableNames == null) deliverableNames = new List<string>();

            //the files variable is null when submitting an in-browser text submission
            if (files != null)
            {
                using (var db = new OSBLEContext())
                {
                    int anchoredDiscussionDocumentCount = 0;
                    foreach (var file in files)
                    {
                        anchoredDiscussionDocumentCount++;
                        if (file != null && file.ContentLength > 0)
                        {
                            DeliverableType type = (DeliverableType)deliverables[i].Type;

                            //jump over all DeliverableType.InBrowserText as they will be handled separately
                            while (type == DeliverableType.InBrowserText)
                            {
                                i++;
                                type = (DeliverableType)deliverables[i].Type;
                            }
                            string fileName = Path.GetFileName(file.FileName);
                            string extension = Path.GetExtension(file.FileName).ToLower();
                            string deliverableName = string.Format("{0}{1}", deliverables[i].Name, extension);

                            string[] allowFileExtensions = GetFileExtensions(type);

                            if (allowFileExtensions.Contains(extension))
                            {
                                if (assignment.Type == AssignmentTypes.CriticalReview ||
                                    assignment.Type == AssignmentTypes.AnchoredDiscussion)
                                {
                                    //TODO: clean this up
                                    AssignmentTeam authorTeam = new AssignmentTeam();
                                    ReviewTeam reviewTeam = new ReviewTeam();

                                    if (assignment.Type == AssignmentTypes.AnchoredDiscussion)
                                    {
                                        authorTeam = new AssignmentTeam
                                        {
                                            Assignment = assignment,
                                            AssignmentID = assignment.ID,
                                            Team = null,
                                            TeamID = anchoredDiscussionDocumentCount,
                                        };

                                        reviewTeam = new ReviewTeam
                                        {
                                            Assignment = assignment,
                                            AssignmentID = assignment.ID,
                                            //AuthorTeam = null,
                                            AuthorTeamID = anchoredDiscussionDocumentCount,
                                            //ReviewingTeam = null,
                                            ReviewTeamID = user.AbstractCourse.ID,
                                        };
                                        assignment.ReviewTeams.Add(reviewTeam);
                                        //db.Entry(assignment).State = System.Data.EntityState.Modified;
                                        db.SaveChanges();
                                    }
                                    else
                                    {
                                        authorTeam = (from at in db.AssignmentTeams
                                                      where at.TeamID == authorTeamID &&
                                                            at.AssignmentID == assignment.PrecededingAssignmentID
                                                      select at).FirstOrDefault();

                                        reviewTeam = (from tm in db.TeamMembers
                                                      join t in db.Teams on tm.TeamID equals t.ID
                                                      join rt in db.ReviewTeams on t.ID equals rt.ReviewTeamID
                                                      where tm.CourseUserID == user.ID
                                                            && rt.AssignmentID == assignment.ID
                                                      select rt).FirstOrDefault();
                                    }

                                    //MG&MK: file system for critical review assignments is laid out a bit differently, so 
                                    //critical review assignments must use different file system functions

                                    //remove all prior files
                                    OSBLE.Models.FileSystem.AssignmentFilePath fs =
                                        Models.FileSystem.Directories.GetAssignment(
                                            user.AbstractCourseID, assignment.ID);
                                    fs.Review(authorTeam.TeamID, reviewTeam.ReviewTeamID)
                                        .File(deliverableName)
                                        .Delete();

                                    if (assignment.Type != AssignmentTypes.AnchoredDiscussion)
                                    // handle assignments that are not anchored discussion
                                    {
                                        //We need to remove the zipfile corresponding to the authorTeamId being sent in as well as the regularly cached zip. 
                                        AssignmentTeam precedingAuthorAssignmentTeam =
                                            (from at in assignment.PreceedingAssignment.AssignmentTeams
                                             where at.TeamID == authorTeamID
                                             select at).FirstOrDefault();
                                        OSBLE.FileSystem.RemoveZipFile(user.AbstractCourse as Course, assignment,
                                            precedingAuthorAssignmentTeam);
                                        OSBLE.FileSystem.RemoveZipFile(user.AbstractCourse as Course, assignment,
                                            assignmentTeam);
                                    }
                                    else //anchored discussion type TODO: this does nothing right now, fix!
                                    {
                                        //We need to remove the zipfile corresponding to the authorTeamId being sent in as well as the regularly cached zip. 
                                        AssignmentTeam precedingAuthorAssignmentTeam =
                                            (from at in assignment.AssignmentTeams
                                             where at.TeamID == authorTeamID
                                             select at).FirstOrDefault();
                                        OSBLE.FileSystem.RemoveZipFile(user.AbstractCourse as Course, assignment,
                                            precedingAuthorAssignmentTeam);
                                        OSBLE.FileSystem.RemoveZipFile(user.AbstractCourse as Course, assignment,
                                            assignmentTeam);
                                    }
                                    //add in the new file
                                    //authorTeamID is the deliverable file counter, and reviewTeamID is the courseID
                                    fs.Review(authorTeam.TeamID, reviewTeam.ReviewTeamID)
                                        .AddFile(deliverableName, file.InputStream);

                                    //unzip and rezip xps files because some XPS generators don't do it right
                                    if (extension.ToLower().CompareTo(".xps") == 0)
                                    {
                                        //XPS documents require the actual file path, so get that.
                                        OSBLE.Models.FileSystem.FileCollection fileCollection =
                                            OSBLE.Models.FileSystem.Directories.GetAssignment(
                                                user.AbstractCourseID, assignment.ID)
                                                .Review(authorTeam.TeamID, reviewTeam.ReviewTeamID)
                                                .File(deliverables[i].Name);
                                        string path = fileCollection.FirstOrDefault();

                                        string extractPath =
                                            Path.Combine(
                                                OSBLE.FileSystem.GetTeamUserSubmissionFolderForAuthorID(true,
                                                    user.AbstractCourse as Course, (int)id, assignmentTeam,
                                                    authorTeam.Team), "extract");
                                        using (ZipFile oldZip = ZipFile.Read(path))
                                        {
                                            oldZip.ExtractAll(extractPath, ExtractExistingFileAction.OverwriteSilently);
                                        }
                                        using (ZipFile newZip = new ZipFile())
                                        {
                                            newZip.AddDirectory(extractPath);
                                            newZip.Save(path);
                                        }
                                    }
                                }
                                else
                                {
                                    //If a submission of any extension exists delete it.  This is needed because they could submit a .c file and then a .cs file and the teacher would not know which one is the real one.
                                    string submission = OSBLE.FileSystem.GetDeliverable(user.AbstractCourse as Course,
                                        assignment.ID, assignmentTeam, deliverables[i].Name, allowFileExtensions);
                                    if (submission != null)
                                    {
                                        FileInfo oldSubmission = new FileInfo(submission);

                                        if (oldSubmission.Exists)
                                        {
                                            oldSubmission.Delete();
                                        }
                                    }
                                    OSBLE.FileSystem.RemoveZipFile(user.AbstractCourse as Course, assignment,
                                        assignmentTeam);
                                    string path =
                                        Path.Combine(
                                            OSBLE.FileSystem.GetTeamUserSubmissionFolder(true,
                                                user.AbstractCourse as Course,
                                                (int)id, assignmentTeam), deliverables[i].Name + extension);
                                    file.SaveAs(path);

                                    //unzip and rezip xps files because some XPS generators don't do it right
                                    if (extension.ToLower().CompareTo(".xps") == 0)
                                    {
                                        string extractPath =
                                            Path.Combine(
                                                OSBLE.FileSystem.GetTeamUserSubmissionFolder(true,
                                                    user.AbstractCourse as Course, (int)id, assignmentTeam), "extract");
                                        using (ZipFile oldZip = ZipFile.Read(path))
                                        {
                                            oldZip.ExtractAll(extractPath, ExtractExistingFileAction.OverwriteSilently);
                                        }
                                        using (ZipFile newZip = new ZipFile())
                                        {
                                            newZip.AddDirectory(extractPath);
                                            newZip.Save(path);
                                        }
                                    }
                                }
                            }
                            deliverableNames.Add(deliverables[i].Name);
                        }
                        else
                        {
                            return true;
                        }
                    }
                    i++;
                }
            }

            // Creates the text files from text boxes
            int j = 0;
            string delName;
            do
            {
                if (request != null)
                    delName = request["desiredName[" + j + "]"];
                else //TODO: change this to releveant string
                    delName = null;

                if (delName != null)
                {
                    string inbrowser;
                    if (request != null)
                    {
                        inbrowser = request["inBrowserText[" + j + "]"];

                        if (inbrowser.Length > 0)
                        {
                            var path =
                                Path.Combine(
                                    OSBLE.FileSystem.GetTeamUserSubmissionFolder(true, user.AbstractCourse as Course,
                                        (int)id, assignmentTeam),
                                    lastName + "_" + firstName + "_" + delName + ".txt");
                            System.IO.File.WriteAllText(path, inbrowser);
                        }
                    }
                }
                j++;
            } while (delName != null);

            return false;
        }
    }
}
