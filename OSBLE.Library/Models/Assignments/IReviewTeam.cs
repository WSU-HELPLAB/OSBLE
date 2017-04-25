using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OSBLE.Models.Assignments
{
    public interface IReviewTeam
    {
        int AssignmentID { get; set; }
        Assignment Assignment { get; set; }

        int AuthorTeamID { get; set; }
        Team AuthorTeam { get; set; }

        int ReviewTeamID { get; set; }
        Team ReviewingTeam { get; set; }
    }
}
