namespace OSBLE.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.ServiceModel.DomainServices.Hosting;
    using System.Web;
    using OSBLE.Controllers;
    using OSBLE.Models.AbstractCourses;
    using OSBLE.Models.AbstractCourses.Course;
    using OSBLE.Models.Assignments.Activities;
    using OSBLE.Models.Courses;
    using OSBLE.Models.Users;
    using OSBLE.Models.ViewModels.ReviewInterface;

    [EnableClientAccess()]
    public class ReviewInterfaceDomainService : OSBLEService
    {
        protected HttpContext context = System.Web.HttpContext.Current;
        protected AbstractAssignmentActivity activity;
        protected TeamUserMember teamUser;

        public ReviewInterfaceDomainService()
        {
            activity = db.AbstractAssignmentActivities.Find((int)context.Session["CurrentActivityID"]);
            teamUser = db.TeamUsers.Find((int)context.Session["TeamUserID"]);

            //Giant if statement
            if (
                //First make sure we have no nulls
                activity == null || teamUser == null || currentCourse as Course == null || currentCourseUser == null

                //then make sure that activity is of the current course and the activity contains the teamUser
                || activity.AbstractAssignment.Category.Course != currentCourse || !activity.TeamUsers.Contains(teamUser)

                //then make sure they have the right role either can grade or they are student looking at their own work
                || (!((currentCourseUser.AbstractRole.CanGrade) || (currentCourseUser.AbstractRole.CanSubmit && teamUser.Contains(currentUserProfile)))))
            {
                throw new Exception("Session did not contain valid IDs for activity or teamUser or currentCourse is not a Course");
            }
        }

        //This needs to get the document locations and return their real location that the client side can open
        public IQueryable<DocumentLocation> GetDocumentLocations()
        {
            /*Int64 temp = 0;
            while (temp < 1000000000)
            {
                temp++;
            }*/
            string path = FileSystem.GetTeamUserSubmissionFolder(false, currentCourse as Course, activity.ID, teamUser);

            List<DocumentLocation> documentsToBeReviewed = new List<DocumentLocation>();
            int i = 0;

            DirectoryInfo di = new DirectoryInfo(path);

            if (di.Exists)
            {
                foreach (FileInfo file in di.GetFiles())
                {
                    string filePath = file.FullName;

                    //get the raw url (not web accessible due to MVC restrictions)
                    string rawUrl = VirtualPathUtility.ToAbsolute("~/" + "FileHandler/GetSubmissionDeliverable?assignmentActivityID=" + activity.ID.ToString() + "&teamUserID=" + teamUser.ID.ToString() + "&fileName=" + file.Name);

                    DocumentLocation location = new DocumentLocation(rawUrl, i, teamUser.Name, AuthorClassification.Student, file.Name);
                    documentsToBeReviewed.Add(location);
                    i++;
                }
            }
            return documentsToBeReviewed.ToArray().AsQueryable();
        }

        public IQueryable<DocumentLocation> GetPeerReviewLocations()
        {
            /*Int64 i = 0;
            while (i < 1000000000)
            {
                i++;
            }*/
            string path = FileSystem.GetTeamUserPeerReview(false, currentCourse as Course, activity.ID, teamUser.ID);

            FileInfo file = new FileInfo(path);

            if (file.Exists)
            {
                string rawUrl = VirtualPathUtility.ToAbsolute("~/FileHandler/GetTeamUserPeerReview?assignmentActivityID=" + activity.ID.ToString() + "&teamUserID=" + teamUser.ID.ToString());
                return (new List<DocumentLocation>() { new DocumentLocation(rawUrl, 100, teamUser.Name, AuthorClassification.Instructor, file.Name) }).AsQueryable();
            }
            else
            {
                //no published file is there a draft?

                path = FileSystem.GetTeamUserPeerReviewDraft(false, currentCourse as Course, activity.ID, teamUser.ID);
                file = new FileInfo(path);
                if (file.Exists)
                {
                    string rawUrl = VirtualPathUtility.ToAbsolute("~/FileHandler/GetTeamUserPeerReview?assignmentActivityID=" + activity.ID.ToString() + "&teamUserID=" + teamUser.ID.ToString());
                    return (new List<DocumentLocation>() { new DocumentLocation(rawUrl, 100, teamUser.Name, AuthorClassification.Instructor, file.Name) }).AsQueryable();
                }
            }

            return null;
        }

        public string AuthorName()
        {
            return currentUserProfile.LastName + ", " + currentUserProfile.FirstName;
        }

        public void UploadFile(string str)
        {
            if (currentCourseUser.AbstractRole.CanGrade)
            {
                using (StreamWriter sw = new StreamWriter(FileSystem.GetTeamUserPeerReview(true, currentCourse as Course, activity.ID, teamUser.ID)))
                {
                    sw.Write(str);
                }
                new NotificationController().SendInlineReviewCompletedNotification(activity, teamUser);
            }
        }

        public void UploadReviewDraft(string str)
        {
            if (currentCourseUser.AbstractRole.CanGrade)
            {
                using (StreamWriter sw = new StreamWriter(FileSystem.GetTeamUserPeerReviewDraft(true, currentCourse as Course, activity.ID, teamUser.ID)))
                {
                    sw.Write(str);
                }
            }
        }

        public IQueryable<CommentCategory> GetCategories(int DocumentID)
        {
            try
            {
                return activity.AbstractAssignment.CommentCategoryConfiguration.Categories.AsQueryable();
            }
            catch
            {
                return null;
            }
        }

        public IQueryable<CommentCategoryOption> GetCommentCategoryOptions(int CommentCategoryID)
        {
            try
            {
                CommentCategory cc = db.CommentCategories.Find(CommentCategoryID);
                return cc.Options.AsQueryable();
            }
            catch
            {
                return null;
            }
        }
    }
}