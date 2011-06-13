using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OSBLE.Models.Assignments.Activities
{
    public class AuthorRebuttalActivity : StudioActivity
    {
        //Need Link to Previous activity 

        public bool AuthorMustAcceptorRefuteEachIssue;
            public bool AuthorMustProvideRationale;
        public bool AuthorMustSayIfIssueWasAddressed;
            public bool AuthorMustDescribeHowAddressed;

    }
}