using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Resources;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;

namespace OSBLE.Areas.AssignmentDetails.Models.HeaderBuilder
{
    public class CriticalReviewStudentDownloadDecorator : HeaderDecorator
    {
        public CourseUser Student { get; set; }

        public CriticalReviewStudentDownloadDecorator(IHeaderBuilder builder, CourseUser student)
            : base(builder)
        {
            Student = student;
        }

        public override DynamicDictionary BuildHeader(Assignment assignment)
        {
            dynamic header = Builder.BuildHeader(assignment);
            header.CRdownload = new DynamicDictionary();

            header.CRdownload.hasPublished = assignment.IsCriticalReviewPublished;
            header.CRdownload.publishedDate = assignment.CriticalReviewPublishDate;

            header.CRdownload.student = Student;
            header.CRdownload.assignmentID = assignment.ID;

            return header;
        }
    }
}