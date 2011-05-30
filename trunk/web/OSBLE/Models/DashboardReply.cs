using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace OSBLE.Models
{
    public class DashboardReply : AbstractDashboard
    {
        public virtual DashboardPost Parent { get; set; }

        public DashboardReply()
            : base()
        {
        }
    }
}