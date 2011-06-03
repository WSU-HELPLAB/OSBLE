using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OSBLE.Models.Assignments.Activities.Scores
{
    public class RubricScore : Score
    {
        // public Rubric Rubric { get; set; }
        // public int RubricID { get; set; }

        public double Points
        {
            get
            {
                return 100;
            }
        }
    }
}