using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Models.Courses.Rubrics;
using OSBLE.Models.Courses;
using OSBLE.Models.Assignments;

namespace OSBLE.Areas.AssignmentWizard.ViewModels
{
    public class RubricSelectionViewModel
    {
        //KEY = Course title
        //VALUE = list of <rubricName, rubricID> pairs
        public Dictionary<string, List<AssignmentRubricInfo>> rubricSelection;

        
        public RubricSelectionViewModel()
        {
            rubricSelection = new Dictionary<string, List<AssignmentRubricInfo>>();
        }

        //return list of the course titles
        public List<string> Courses()
        {
            return rubricSelection.Keys.ToList();
        }

        public void AddCourse(Course course)
        {
            List<AssignmentRubricInfo> ari = new List<AssignmentRubricInfo>();
            if (course != null)
            {
                foreach (Assignment assignment in course.Assignments)
                {
                    ari.Add(new AssignmentRubricInfo(
                        assignment.Rubric.Description,
                        assignment.AssignmentName,
                        assignment.Rubric.ID
                        ));
                }
                rubricSelection.Add(course.Name, ari);
            }
        }
    }

    public class AssignmentRubricInfo
    {
        public string RubricDescription { get; set; }
        public string AssignmentName { get; set; }
        public int RubricID { get; set; }

        public AssignmentRubricInfo(string rubricDescription, string assignmentName, int rubricID)
        {
            RubricDescription = rubricDescription;
            AssignmentName = assignmentName;
            RubricID = rubricID;
        }
    }
}
