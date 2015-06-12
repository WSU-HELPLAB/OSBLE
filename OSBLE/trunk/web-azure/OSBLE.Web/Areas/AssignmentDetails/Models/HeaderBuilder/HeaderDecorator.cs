using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Resources;
using OSBLE.Models.Assignments;
using OSBLE.Areas.AssignmentDetails.Models.HeaderBuilder;
using OSBLE.Models.Courses;

namespace OSBLE.Areas.AssignmentDetails.Models.HeaderBuilder
{
    public abstract class HeaderDecorator : IHeaderBuilder
    {
        protected IHeaderBuilder Builder { get; set; }

        public HeaderDecorator(IHeaderBuilder builder)
        {
            Builder = builder;
        }

        public abstract DynamicDictionary BuildHeader(Assignment assignment);
    }
}