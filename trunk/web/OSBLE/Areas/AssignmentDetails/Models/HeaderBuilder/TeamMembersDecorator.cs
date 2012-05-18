using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Resources;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;
// Displayed for students when assignment.hasTeams == true
// shows the team name, and profile pic of other team members.
namespace OSBLE.Areas.AssignmentDetails.Models.HeaderBuilder
{
    public class TeamMembersDecorator : HeaderDecorator
    {
        public CourseUser Client { get; set; }
        public TeamMembersDecorator(IHeaderBuilder builder, CourseUser client)
            : base(builder)
        {
            Client = client;
        }

        public override DynamicDictionary BuildHeader(Assignment assignment)
        {
            dynamic header = Builder.BuildHeader(assignment);
            header.Team = new DynamicDictionary();

            //create list to store team members
            List<TeamMember> teamMembers = new List<TeamMember>();

            // get name of relevant team
            List<TeamMember> allMembers = assignment.AssignmentTeams.SelectMany(at => at.Team.TeamMembers).ToList();
            TeamMember member = allMembers.Where(m => m.CourseUserID == Client.ID).FirstOrDefault();
            if (member != null)
            {
                header.Team.Name = member.Team.Name;
            }

            //populate list of team members, (except for the current user)
            foreach (TeamMember user in member.Team.TeamMembers)
            {
                if (user.CourseUser.ID != Client.ID)
                {
                    teamMembers.Add(user);
                }
            }
            header.Team.TeamMembers = teamMembers;

            // store the team ID
            header.Team.ID = member.TeamID;
            
            return header;
        }
    }
}
