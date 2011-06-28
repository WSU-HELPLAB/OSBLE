using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Assignments
{
    public interface ILatePolicy
    {
        int MinutesLateWithNoPenalty { get; set; }

        int PercentPenalty { get; set; }

        int HoursLatePerPercentPenalty { get; set; }

        int HoursLateUntilZero { get; set; }
    }
}