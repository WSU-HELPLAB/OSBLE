﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Resources;
using OSBLE.Models.Assignments;

namespace OSBLE.Areas.AssignmentDetails.Models.HeaderBuilder
{
    public class CriticalReviewStudentDownloadDecorator : HeaderDecorator
    {
        public CriticalReviewStudentDownloadDecorator(IHeaderBuilder builder)
            : base(builder)
        {
        }

        public override DynamicDictionary BuildHeader(Assignment assignment)
        {
            dynamic header = Builder.BuildHeader(assignment);
            header.dueDate = assignment.DueDate;
            return header;
        }
    }
}