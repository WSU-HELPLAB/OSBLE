using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Resources;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;
using OSBLE.Models.Courses.Rubrics;

namespace OSBLE.Areas.AssignmentDetails.Models.HeaderBuilder
{
    public class RubricGradeDecorator : HeaderDecorator
    {

        public CourseUser Student { get; set; }
        public RubricEvaluation RubricEvaluation { get; set; }

        public RubricGradeDecorator(IHeaderBuilder builder, CourseUser student, List<RubricEvaluation> evaluations)
            : base(builder)
        {
            Student = student;
            RubricEvaluation = evaluations.Where(eval => eval.RecipientID == student.ID).FirstOrDefault();
        }

        public override DynamicDictionary BuildHeader(Assignment assignment)
        {

            dynamic header = Builder.BuildHeader(assignment);
            header.AssignmentDetails = new DynamicDictionary();


            //If the Rubric has been evaluated and is published, calculate the rubric grade % to display to the student
            if (RubricEvaluation != null && RubricEvaluation.IsPublished && RubricEvaluation.CriterionEvaluations.Count > 0)
            {
                header.AssignmentDetails.hasGrade = true;
                header.AssignmentDetails.assignmentID = assignment.ID;
                header.AssignmentDetails.cuID = Student.ID;
                header.AssignmentDetails.DisplayValue = RubricEvaluation.GetGradeAsPercent();
            }
            else
            {
                header.AssignmentDetails.DisplayValue = "Not Graded";
                header.AssignmentDetails.hasGrade = false;
            }   
            return header;
        }
    }
}
