using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.Web.Mvc;
using OSBLE.Models.Assignments.Activities;

namespace OSBLE.Services
{
    [ServiceContract(Namespace = "")]
    [SilverlightFaultBehavior]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    [Authorize]
    public class ReviewInterfaceService : OSBLEService
    {
        [OperationContract]
        public void DoWork()
        {
        }

        [OperationContract]
        public string GetFileLocation(int abstractAssignmentActivityID, int submissionID)
        {
            AbstractAssignmentActivity activity = db.AbstractAssignmentActivities.Find(abstractAssignmentActivityID);

            if (activity.AbstractAssignment.Category.CourseID == CurrentCourseUser.AbstractCourseID)
            {
                var teamUser = (from c in activity.TeamUsers where c.Contains(currentUserProfile) == true select c).FirstOrDefault();
            }
            return null;
        }
    }
}