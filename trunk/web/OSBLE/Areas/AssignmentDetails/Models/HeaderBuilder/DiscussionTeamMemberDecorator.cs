using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Resources;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;
using OSBLE.Models.Users;
// Displayed for students when assignment.hasTeams == true
// shows the team name, and profile pic of other team members.
namespace OSBLE.Areas.AssignmentDetails.Models.HeaderBuilder
{
    public class DiscussionTeamMemberDecorator : HeaderDecorator
    {
        public CourseUser Client { get; set; }
        public DiscussionTeamMemberDecorator(IHeaderBuilder builder, CourseUser client)
            : base(builder)
        {
            Client = client;
        }

        public override DynamicDictionary BuildHeader(Assignment assignment)
        {
            dynamic header = Builder.BuildHeader(assignment);
            header.DiscussionTeam = new DynamicDictionary();

            //create list to store team members
            List<List<UserProfile>> ListOfListOfMembers = new List<List<UserProfile>>();
            List<string> ListOfTeamNames = new List<string>();
            List<int> ListOfTeamIDs = new List<int>();

            //Finding "Client's" Discussion Team(s). In some assignment types (CRD) users can be part of multiple DiscussionTeams
            foreach (DiscussionTeam dt in assignment.DiscussionTeams)
            {
                foreach (TeamMember tm in dt.GetAllTeamMembers())
                {
                    if (tm.CourseUserID == Client.ID)
                    {
                        //Adding all the UserProfiles from the current discussion team to a list (used in view)
                        //as well as the team name and ID, then moving onto next discussion team via break.
                        ListOfListOfMembers.Add(
                            (from tm2 in dt.GetAllTeamMembers()
                             where tm2.CourseUserID != Client.ID
                             select tm2.CourseUser.UserProfile).ToList()
                            );
                        ListOfTeamNames.Add(dt.TeamName);

                        //issue: mailing is by team ID...in this case we have two team IDs..the team and author teams....
                        //perhaps to resolve this, have the CreateTeamMail accept an optional parameter of author team,
                        //or to make things even easier, simple have CreateDiscussionTeamMail(DiscussionTeam.ID)
                        ListOfTeamIDs.Add(dt.ID);
                        break;
                    }
                }
            }

            header.DiscussionTeam.ListOfListOfMembers = ListOfListOfMembers;
            header.DiscussionTeam.ListOfTeamNames = ListOfTeamNames;
            header.DiscussionTeam.ListOfTeamIDs = ListOfTeamIDs;

            return header;
        }
    }
}
