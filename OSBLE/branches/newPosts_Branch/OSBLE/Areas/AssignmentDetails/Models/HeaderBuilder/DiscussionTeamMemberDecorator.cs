using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Resources;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;
using OSBLE.Models.Users;
using OSBLE.Models.ViewModels;
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

            //create list of lists to store all the posters. A list of stringsfor team names, and a list of ints for all the team's ids
            List<List<Poster>> ListOfListOfPosters = new List<List<Poster>>();
            List<string> ListOfTeamNames = new List<string>();
            List<int> ListOfTeamIDs = new List<int>();
            List<bool> ListOfHideMailIcon = new List<bool>();
            List<int> ListOfNewPostCounts = new List<int>();

            //Finding "Client's" Discussion Team(s). In some assignment types (CRD) users can be part of multiple DiscussionTeams
            foreach (DiscussionTeam dt in assignment.DiscussionTeams.OrderBy(dt => dt.TeamName).ToList())
            {
                foreach (TeamMember tm in dt.GetAllTeamMembers())
                {
                    if (tm.CourseUserID == Client.ID) //Checking if Client is a member within the DiscussionTeam
                    {

                        //generating a list of "Posters" which is a view model used to display courseusers with proper anonmization. 
                        bool hideMail = false;
                        List<Poster> posters = new List<Poster>();
                        if (assignment.Type == AssignmentTypes.CriticalReviewDiscussion)
                        {
                            List<int> AuthorCourseUserIds = dt.AuthorTeam.TeamMembers.Select(teamMem => teamMem.CourseUserID).ToList();
                            List<int> ReviewerCourseUserIds = dt.Team.TeamMembers.Where(teamMem => teamMem.CourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.Student).Select(teamMem => teamMem.CourseUserID).ToList();
                            bool clientIsAuthor = AuthorCourseUserIds.Contains(Client.ID);
                            bool clientIsReviewer = ReviewerCourseUserIds.Contains(Client.ID);

                            foreach (CourseUser cu in dt.GetAllTeamMembers().Select(teamMem => teamMem.CourseUser).ToList())
                            {
                                bool isAuthor = AuthorCourseUserIds.Contains(cu.ID);
                                bool isReviewer = ReviewerCourseUserIds.Contains(cu.ID);
                                string RoleName = "";
                                if (isAuthor && isReviewer)
                                {
                                    RoleName = "Author/Reviewer";
                                }
                                else if (isReviewer)
                                {
                                    RoleName = "Reviewer";
                                }
                                else if (isAuthor)
                                {
                                    RoleName = "Author";
                                }
                                Poster poster = new Poster()
                                {
                                    Anonymize = DiscussionViewModel.AnonymizeNameForCriticalReviewDiscussion(cu, Client, assignment, isAuthor, isReviewer, clientIsAuthor, clientIsReviewer),
                                    CourseUser = cu,
                                    HideRole = assignment.DiscussionSettings.HasHiddenRoles,
                                    RoleName = RoleName,
                                    UserProfile = cu.UserProfile

                                };
                                hideMail = hideMail || poster.Anonymize; //If any poster is Anonymized, then hideMail should true.
                                posters.Add(poster);
                            }
                        }
                        else
                        {
                            foreach (CourseUser cu in dt.GetAllTeamMembers().Select(teamMem => teamMem.CourseUser).ToList())
                            {
                                Poster poster = new Poster()
                                {
                                    Anonymize = DiscussionViewModel.AnonymizeNameForDiscussion(cu, Client, assignment.DiscussionSettings),
                                    CourseUser = cu,
                                    HideRole = assignment.DiscussionSettings.HasHiddenRoles,
                                    RoleName = "",
                                    UserProfile = cu.UserProfile

                                };
                                hideMail = hideMail || poster.Anonymize; //If any poster is Anonymized, then hideMail should true.
                                posters.Add(poster);
                            }
                        }

                        ListOfNewPostCounts.Add(dt.GetNewPostsCount(Client.ID));
                        ListOfHideMailIcon.Add(hideMail);
                        ListOfListOfPosters.Add(posters);
                        ListOfTeamNames.Add(dt.TeamName);
                        ListOfTeamIDs.Add(dt.ID);
                        break;
                    }
                }
            }

            header.DiscussionTeam.ListOfNewPostCounts = ListOfNewPostCounts;
            header.DiscussionTeam.ListOfHideMailIcon = ListOfHideMailIcon; 
            header.DiscussionTeam.Assignment = assignment;
            header.DiscussionTeam.ListOfListOfPosters = ListOfListOfPosters;
            header.DiscussionTeam.ListOfTeamNames = ListOfTeamNames;
            header.DiscussionTeam.ListOfTeamIDs = ListOfTeamIDs;

            return header;
        }
    }
}
