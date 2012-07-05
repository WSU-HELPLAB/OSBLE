using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;

namespace OSBLE.Models.ViewModels
{
    public class TeamEvaluationViewModel
    {
        public List<TeamEvaluation> MyRecievedEvals;
        public Score MyScore;
        public double Multiplier;
        public CourseUser Recipient;
    }
}