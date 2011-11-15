﻿using System.ComponentModel;
using OSBLE.Models.Assignments;
using OSBLE.Models.Assignments.Activities;
using OSBLE.Models.AbstractCourses.Course;

namespace OSBLE.Models.ViewModels
{
    public class BasicAssignmentViewModel
    {
        public BasicAssignmentViewModel()
        {
            Submission = new SubmissionActivity();
            Stop = new StopActivity();
            Assignment = new StudioAssignment();
            isGradable = true;
        }

        [DisplayName("Use OSBLE rubric")]
        public bool UseRubric { get; set; }

        public SubmissionActivity Submission { get; set; }

        public StopActivity Stop { get; set; }

        public StudioAssignment Assignment { get; set; }

        public SilverlightObject TeamCreation { get; set; }

        public SilverlightObject RubricCreation { get; set; }

        public string SerializedTeams { get; set; }

        public string SerializedTeamMembersJSON { get; set; }

        public bool isGradable { get; set; }
    }
}