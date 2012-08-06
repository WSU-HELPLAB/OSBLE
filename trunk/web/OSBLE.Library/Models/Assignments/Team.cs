using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using OSBLE.Models.Users;
using OSBLE.Models.Courses;

namespace OSBLE.Models.Assignments
{
    public class Team : IComparable, IEquatable<Team>, IEqualityComparer<Team>
    {
        [Key]
        public int ID { get; set; }
        public string Name { get; set; }

        public virtual IList<TeamMember> TeamMembers { get; set; }
        public virtual IList<AssignmentTeam> UsedAsAssignmentTeam { get; set; }

        [NotMapped]
        public IList<Assignment> UsedInAssignments
        {
            get
            {
                List<Assignment> assignments = new List<Assignment>();
                var assignmentTeams = UsedAsAssignmentTeam.Select(at => at.Assignment);
                assignments = assignmentTeams
                              .Distinct()
                              .ToList();
                return assignments;
            }
        }

        public Team()
        {
            TeamMembers = new List<TeamMember>();
            UsedAsAssignmentTeam = new List<AssignmentTeam>();
        }

        public int CompareTo(object obj)
        {
            Team other = obj as Team;
            if (other == null)
            {
                return -1;
            }
            if (other.ID == 0 || this.ID == 0)
            {
                return this.Name.CompareTo(other.Name);
            }
            else
            {
                return this.ID.CompareTo(other.ID);
            }
        }

        public bool Equals(Team other)
        {
            if (this.CompareTo(other) == 0)
            {
                return true;
            }
            return false;
        }

        public bool Equals(Team x, Team y)
        {
            if (x.CompareTo(y) == 0)
            {
                return true;
            }
            return false;
        }

        public int GetHashCode(Team obj)
        {
            if (obj.ID != 0)
            {
                return obj.ID.GetHashCode();
            }
            else
            {
                return obj.Name.GetHashCode();
            }
        }

        /// <summary>
        /// Converts the list of team members into a string for display purposes.
        /// Will contain the name(s) of each individual on the team.
        /// </summary>
        /// <param name="seperator"></param>
        /// <returns></returns>
        public string TeamMemberString(string separator = ";")
        {
            string[] names = (from member in this.TeamMembers
                              select member.CourseUser.DisplayName()).ToArray<string>();
            return string.Join(separator, names);
        }

        /// <summary>
        /// Converts the list of team members into a string for display purposes.
        /// Will contain the name(s) of each individual on the team.
        /// </summary>
        /// <param name="seperator"></param>
        /// <returns></returns>
        public string TeamMemberString(AbstractRole viewerRole, string separator = "; ")
        {
            string[] names = (from member in this.TeamMembers
                              select member.CourseUser.DisplayName(viewerRole)).ToArray<string>();
            return string.Join(separator, names);
        }

        public string DisplayName(AbstractRole viewerRole)
        {
            if (viewerRole.Anonymized) // observer
            {
                // will want to change this.ID to this.ID % with # teams Mabye
                return "Team " + this.ID;
            }
            else
            {
                return this.Name;
            }
        }
    }
}