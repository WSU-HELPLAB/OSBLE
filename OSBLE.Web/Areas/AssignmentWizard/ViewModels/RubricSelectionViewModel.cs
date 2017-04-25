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
        public Dictionary<int, CourseViewModel> rubricSelection;

        public RubricSelectionViewModel()
        {
            rubricSelection = new Dictionary<int, CourseViewModel>();
        }

        //return list of the course names
        public List<string> CourseNames()
        {
            return rubricSelection.Values.Select(r => r.CourseName).ToList();
        }

        public List<int> CourseIds()
        {
            return rubricSelection.Keys.ToList();
        }

        public string GetCourseName(int courseId)
        {
            return rubricSelection[courseId].CourseName;
        }

        public void AddCourse(Course course)
        {
            if (course != null)
            {
                if (course.Assignments.Where(a => a.HasRubric).Count() > 0 || course.Assignments.Where(a => a.HasStudentRubric).Count() > 0)
                {
                    rubricSelection.Add(course.ID, new CourseViewModel(course));
                }    
            }
        }
    }

    public class CourseViewModel
    {
        public List<RubricViewModel> rubricViewModel;
        public string CourseName;

        public CourseViewModel(Course course)
        {
            rubricViewModel = new List<RubricViewModel>();
           
            foreach (Assignment assignment in course.Assignments)
            {
                try
                {
                    if (assignment.HasRubric)
                    {
                        rubricViewModel.Add(new RubricViewModel(
                            assignment.Rubric.Description,
                            assignment.AssignmentName,
                            assignment.ID,
                            assignment.RubricID,
                            assignment.StudentRubricID,
                            assignment.Rubric.EnableHalfStep,
                            assignment.Rubric.EnableQuarterStep
                            ));
                    }

                    if (assignment.HasStudentRubric)
                    {
                        rubricViewModel.Add(new RubricViewModel(
                            assignment.StudentRubric.Description,
                            assignment.AssignmentName,
                            assignment.ID,
                            assignment.RubricID,
                            assignment.StudentRubricID,
                            assignment.Rubric.EnableHalfStep,
                            assignment.Rubric.EnableQuarterStep
                            ));
                    }
                }
                catch (Exception e)
                {
                     //TODO: handle this exception.                   
                }                
            }

            CourseName = course.Name;
        }
    }

    public class RubricViewModel
    {
        public string RubricDescription { get; set; }
        public string AssignmentName { get; set; }
        public int AssignmentID { get; set; }
        public int? RubricID { get; set; }
        public int? StudentRubricID { get; set; }
        public bool EnableHalfStep { get; set; }
        public bool EnableQuarterStep { get; set; }

        public RubricViewModel(string rubricDescription, 
                string assignmentName, 
                int assignmentID, 
                int? rubricID,
                int? studentRubricID,
                bool enableHalfStep,
                bool enableQuarterStep)
        {
            RubricDescription = rubricDescription;
            AssignmentName = assignmentName;
            AssignmentID = assignmentID;
            RubricID = rubricID;
            StudentRubricID = studentRubricID;
            EnableHalfStep = enableHalfStep;
            EnableQuarterStep = enableQuarterStep;
        }
    }
}
