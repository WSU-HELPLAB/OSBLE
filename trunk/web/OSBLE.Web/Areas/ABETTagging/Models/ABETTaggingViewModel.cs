// Created by Evan Olds for the OSBLE project at WSU
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using OSBLE.Areas.ABETTagging.Models;
using OSBLE.Models.Assignments;
using OSBLE.Controllers;
using OSBLE.Areas.AssignmentDetails.ViewModels;
using OSBLE.Attributes;
using OSBLE.Models.Users;
using OSBLE.Models.FileSystem;

namespace OSBLE.Areas.ABETTagging.Models
{
    public class ABETTaggingViewModel
    {
        private Assignment m_a;

        private List<Submission> m_subs = new List<Submission>();

        public ABETTaggingViewModel(Assignment theAssignment, int roleID)
        {
            m_a = theAssignment;

            // For ABET tagging, we want to be able to tag each assignment submission. 
            // Based on the assignment type, assignments could be submitted by 
            // individual students or by teams.
            // I believe that either way we can use the assignment teams list
            foreach (AssignmentTeam team in m_a.AssignmentTeams)
            {
                if (team.GetSubmissionTime() != null)
                {
                    Submission s = new Submission();
                    foreach (TeamMember tm in team.Team.TeamMembers)
                    {
                        s.TeamMembers.Add(tm.CourseUser.DisplayName(roleID));
                    }

                    // Find the file storage for the team submission
                    AttributableFilesFilePath afp =
                        (new OSBLE.Models.FileSystem.FileSystem()).
                        Course(m_a.CourseID.Value).
                        Assignment(m_a.ID).
                        Submission(team.TeamID) as AttributableFilesFilePath;

                    GetOptionsHTML(afp, s, team.TeamID);

                    m_subs.Add(s);
                }
            }
        }

        public Assignment Assignment
        {
            get { return m_a; }
        }

        private bool GetOptionsHTML(AttributableFilesFilePath path, Submission s, int teamID)
        {
            // The attributes associated with the assignment submission files 
            // will potentially have ABET outcome attributes in them. This 
            // determines whether or not an outcome is checked.
            FileCollection fc = path.AllFiles();
            AttributableFile first = null;
            if (0 != fc.Count)
            {
                IEnumerator<string> enumeratificator = fc.GetEnumerator();
                enumeratificator.MoveNext();
                first = path.GetFile(enumeratificator.Current);
            }
            
            // Make sure we found the submitted file
            if (0 == fc.Count || null == first)
            {
                s.CategoryOptionsHTML = "(no outcomes available)";
                s.ProficiencyOptionsHTML = "(no options available)";
                return false;
            }

            StringBuilder sb;
            
            // Build the HTML string for the check boxes for each category
            if (0 == m_a.ABETOutcomes.Count)
            {
                s.CategoryOptionsHTML = "(no outcomes available)";
            }
            else
            {
                sb = new StringBuilder();
                for (int i = 0; i < m_a.ABETOutcomes.Count; i++ )
                {
                    AbetAssignmentOutcome outcome = m_a.ABETOutcomes[i];

                    bool hasOutcome = first.ContainsSysAttr(
                        "ABETOutcome", outcome.Outcome);
                    sb.AppendFormat(
                        "<input id=\"{0}\" name=\"{0}\" type=\"checkbox\" value=\"{2}\" {1}/>{2}<br />",
                        "outcome" + teamID.ToString() + "_" + i.ToString(),
                        (hasOutcome ? "checked " : string.Empty),
                        outcome.Outcome);
                }
                s.CategoryOptionsHTML = sb.ToString();
            }

            // Build the HTML string for the radio buttons for proficiency levels
            string grpName = "radios" + teamID.ToString();
            sb = new StringBuilder("<input type=\"radio\" name=\"" + 
                grpName + "\" />None<br />");
            string[] levels = new string[] { "Low", "Medium", "High" };
            foreach (string level in levels)
            {
                if (first.ContainsSysAttr("ABETProficiencyLevel", level))
                {
                    sb.AppendFormat(
                        "<input type=\"radio\" name=\"{0}\" value=\"{1}\" checked />{1}<br>",
                        grpName, level);
                }
                else
                {
                    sb.AppendFormat(
                        "<input type=\"radio\" name=\"{0}\" value=\"{1}\" />{1}<br>",
                        grpName, level);
                }
                sb.AppendLine();
            }
            s.ProficiencyOptionsHTML = sb.ToString();

            return true;
        }

        public string NameHeader
        {
            get
            {
                return m_a.HasTeams ? "Team Members" : "Name";
            }
        }

        public IList<Submission> Submissions
        {
            get { return m_subs; }
        }

        public class Submission
        {
            public string CategoryOptionsHTML;

            public string ProficiencyOptionsHTML;
            
            /// <summary>
            /// Will just be a single student name if the assignment is not done 
            /// in teams (or if it is done in teams and the team has only one 
            /// member).
            /// </summary>
            public List<string> TeamMembers = new List<string>();
        }
    }
}