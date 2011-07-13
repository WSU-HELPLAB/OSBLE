namespace OSBLE.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.ServiceModel.DomainServices.Hosting;
    using System.Web;
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

        private ReviewInterfaceDomainService()
        {
            activity = db.AbstractAssignmentActivities.Find((int)context.Session["CurrentActivityID"]);
            teamUser = db.TeamUsers.Find((int)context.Session["TeamUserID"]);

            if (activity == null || teamUser == null)
            {
                throw new Exception("Session did not contain valid IDs for activity or teamUser");
            }
        }

        //This needs to get the document locations and return their real location that the client side can open
        public IQueryable<DocumentLocation> GetDocumentLocations()
        {
            string path = FileSystem.GetTeamUserSubmissionFolder(false, currentCourse as Course, activity.ID, teamUser);

            List<DocumentLocation> documentsToBeReviewed = new List<DocumentLocation>();
            int i = 0;
            foreach (FileInfo file in new DirectoryInfo(path).GetFiles())
            {
                DocumentLocation location = new DocumentLocation(file.FullName, i, teamUser.Name, AuthorClassification.Student);
                i++;
            }
            return documentsToBeReviewed.ToArray().AsQueryable();
        }

        public IQueryable<DocumentLocation> GetPeerReviewLocations()
        {
            return null;
        }

        public void UploadFile(string str)
        {
            StreamWriter sw = new StreamWriter(FileSystem.GetTeamUserReviewFolderLocation(currentCourse as Course, activity.AbstractAssignmentID, teamUser.ID) + "/PeerReview.xml");
            sw.Write(str);
            sw.Close();
        }
    }
}