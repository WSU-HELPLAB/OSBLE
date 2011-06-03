using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Models.Assignments.Activities.Scores;
using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Assignments.Activities
{
    /// <summary>
    /// Used for practice peer review assignments, where the instructor can submit a single set of deliverables
    /// for the entire class to review.
    /// </summary>
    public class SingleSubmissionActivity : AssignmentActivity, IHasDeliverables
    {
        public new int PointsPossible
        {
            get { return 0; }
        }

        public override ICollection<Score> Scores
        {
            get { return new List<Score>(); }
        }

        [Display(Name = "Deliverables")]
        public virtual ICollection<Deliverable> Deliverables { get; set; }
    }
}