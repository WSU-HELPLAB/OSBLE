using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Assignments
{
    /// <summary>
    /// Grade Assignments are used for single graded items, such as a day of participation, 
    /// an exam, quiz, etc.
    /// </summary>
    public class GradeAssignment : AbstractAssignment
    {
        [NotMapped]
        public new DateTime? EndDate
        {
            get { return null; }
        }
    }
}