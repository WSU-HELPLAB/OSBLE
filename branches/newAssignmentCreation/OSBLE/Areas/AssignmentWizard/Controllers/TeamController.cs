using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Models.ViewModels;
using OSBLE.Models.Users;
using OSBLE.Models.Courses;

namespace OSBLE.Areas.AssignmentWizard.Controllers
{
    public class TeamController : WizardBaseController
    {

        private TeamCreationViewModel BuildViewModel()
        {
            TeamCreationViewModel viewModel = new TeamCreationViewModel();
            viewModel.Assignment = Assignment;
            viewModel.Students = (from cu in db.CoursesUsers
                                  join up in db.UserProfiles on cu.UserProfileID equals up.ID
                                  where cu.AbstractRoleID == (int)CourseRole.CourseRoles.Student
                                  && cu.AbstractCourseID == activeCourse.AbstractCourseID
                                  orderby up.LastName, up.FirstName
                                  select up).ToList();
            return viewModel;
        }

        public override string ControllerName
        {
            get { return "Team"; }
        }

        public override string ControllerDescription
        {
            get { return "The assignment is team-based"; }
        }

        public override ICollection<WizardBaseController> Prerequisites
        {
            get 
            {
                List<WizardBaseController> prereqs = new List<WizardBaseController>();
                prereqs.Add(new BasicsController());
                return prereqs;
            }
        }

    }
}
