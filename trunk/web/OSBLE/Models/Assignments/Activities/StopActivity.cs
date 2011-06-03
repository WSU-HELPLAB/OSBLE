using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using OSBLE.Models.Assignments.Activities.Scores;

namespace OSBLE.Models.Assignments.Activities
{
    /// <summary>
    /// This activity marks the end of another activity (due date)
    /// There should be one of these at the end of every Assignment 
    /// (except GradeAssignment, which only returns a StartDate)
    /// </summary>
    public class StopActivity : AssignmentActivity
    {
        public new int PointsPossible
        {
            get { return 0; }
        }

        public StopActivity()
        {
            DateTime dateTimeNow = DateTime.Now;

            //The default is a week from today a second before midnight
            ReleaseDate = new DateTime(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day, 23, 59, 59);
            ReleaseDate = ReleaseDate.AddDays(7);
        }

        public override ICollection<Score> Scores
        {
            get { return new List<Score>(); }
        }
    }
}