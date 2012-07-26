using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Resources;
using OSBLE.Models.Assignments;

namespace OSBLE.Areas.AssignmentDetails.Models.TableBuilder
{
    public class LatePenaltyTableDecorator : TableDecorator
    {
        public LatePenaltyTableDecorator(ITableBuilder builder)
            : base(builder)
        {
        }

        public override DynamicDictionary BuildTableForTeam(IAssignmentTeam assignmentTeam)
        {
            dynamic data = Builder.BuildTableForTeam(assignmentTeam);

            double deductionPercent = 0;
            DateTime? SubmissionTime = FileSystem.GetSubmissionTime(assignmentTeam);
            TimeSpan lateness;
            if (SubmissionTime != null)
            {
                lateness = assignmentTeam.Assignment.DueDate - (DateTime)SubmissionTime;
            }
            else //if the assignment has not been submitted, use the current time to calculate late penalty.
            {
                lateness = assignmentTeam.Assignment.DueDate - DateTime.Now;
            }

            //The document is too late to be accepted. Therefor 100% deduction
            if (lateness.TotalHours >= assignmentTeam.Assignment.HoursLateWindow)
            {
                deductionPercent = 100;
            }
            else if (lateness.TotalHours <= 0) //The document wasnt late at all
            {
                deductionPercent = 0;
            }
            else //The document was late, but less than HoursLateWindow
            {
                int numberOfDeductions = lateness.Hours / (int)assignmentTeam.Assignment.HoursPerDeduction;
                deductionPercent = numberOfDeductions * assignmentTeam.Assignment.DeductionPerUnit;
                if (deductionPercent > 100)
                {
                    deductionPercent = 100;
                }
            }

            data.LatePenaltyPercent = (deductionPercent/100.0).ToString("P");
            return data;
        }
    }
}