using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Models.Assignments;
using OSBLE.Areas.AssignmentDetails.Models.HeaderBuilder;
using OSBLE.Areas.AssignmentDetails.Models.TableBuilder;
using OSBLE.Models.AbstractCourses.Course;
using OSBLE.Models.Courses;

namespace OSBLE.Areas.AssignmentDetails.ViewModels
{
    public class AssignmentDetailsViewModel
    {
        public Assignment CurrentAssignment { get; set; }
        public CourseUser Client { get; set; }

        public List<string> HeaderViews { get; set; }
        public IHeaderBuilder HeaderBuilder { get; set; }

        public Dictionary<IAssignmentTeam, ITableBuilder> TeamTableBuilders { get; set; }
        public Dictionary<string, string> TableColumnHeaders { get; set; }

        public AssignmentDetailsViewModel()
        {
            HeaderViews = new List<string>();
            TeamTableBuilders = new Dictionary<IAssignmentTeam, ITableBuilder>();
            TableColumnHeaders = new Dictionary<string, string>();
        }
    }
}