using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Resources;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;

namespace OSBLE.Areas.AssignmentDetails.Models.HeaderBuilder
{
    public class StudentGradeDecorator : HeaderDecorator
    {

        public CourseUser Student { get; set; }

        public StudentGradeDecorator(IHeaderBuilder builder, CourseUser student)
            : base(builder)
        {
            Student = student;
        }

        public override DynamicDictionary BuildHeader(Assignment assignment)
        {
            dynamic header = Builder.BuildHeader(assignment);
            header.AssignmentDetails = new DynamicDictionary();
            var score = (from assignmentScore in assignment.Scores
                            where assignmentScore.CourseUser.ID == Student.ID
                            select assignmentScore).FirstOrDefault();
            if (score == null)
            {
                header.AssignmentDetails.grade = "No Grade";
                header.AssignmentDetails.hasGrade = false;
            }
            else
            {
                header.AssignmentDetails.grade = score.getGradeAsPercent(assignment.PointsPossible).ToString();
                header.AssignmentDetails.hasGrade = true;
            }

            header.AssignmentDetails.hasRubric = assignment.HasRubric;
            header.AssignmentDetails.assignmentID = assignment.ID;
            header.AssignmentDetails.cuID = Student.ID;
            return header;
        }


    }
}
