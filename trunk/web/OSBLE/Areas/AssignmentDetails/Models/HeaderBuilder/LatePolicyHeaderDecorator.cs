using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Resources;
using OSBLE.Models.Assignments;

namespace OSBLE.Areas.AssignmentDetails.Models.HeaderBuilder
{
    public class LatePolicyHeaderDecorator : HeaderDecorator
    {
        public LatePolicyHeaderDecorator(IHeaderBuilder builder)
            : base(builder)
        {
        }

        public override DynamicDictionary BuildHeader(Assignment assignment)
        {
            dynamic header = Builder.BuildHeader(assignment);
            header.LatePolicy = new DynamicDictionary();
            header.Assignment = assignment;
            return header;
        }
    }
}