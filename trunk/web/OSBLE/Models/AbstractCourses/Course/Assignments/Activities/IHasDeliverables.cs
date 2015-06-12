using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Assignments.Activities
{
    public interface IHasDeliverables
    {
        ICollection<Deliverable> Deliverables { get; set; }
    }
}