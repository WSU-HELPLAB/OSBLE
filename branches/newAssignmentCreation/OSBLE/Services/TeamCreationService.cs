
namespace OSBLE.Services
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.ServiceModel.DomainServices.Hosting;
    using System.ServiceModel.DomainServices.Server;
    using OSBLE.Models.Users;
    using OSBLE.Models.Courses;

    [EnableClientAccess()]
    public class TeamCreationService : OSBLEService
    {
        public TeamUserMember DummyTeamUserMember()
        {
            throw new NotImplementedException("You're not supposed to use this!");
        }

        public TeamMember DummyTeamMember()
        {
            throw new NotImplementedException("You're not supposed to use this!");
        }

        public UserMember DummyUserMember()
        {
            throw new NotImplementedException("You're not supposed to use this!");
        }

        public IQueryable<TeamMember> GetAssignmentTeams(int assignmentId)
        {
            var query = (from activity in db.AbstractAssignmentActivities
                         where activity.AbstractAssignmentID == assignmentId
                         && activity.TeamUsers.Count > 0
                         select activity.TeamUsers).FirstOrDefault();
            return query.Where(q => q is TeamMember).Select(q => q as TeamMember).AsQueryable();
        }

        public IQueryable<UserMember> GetAssignmentUsers(int assignmentId)
        {
            //if we don't have a team assignment, then this query will return what
            //we're looking for
            var baseQuery = (from activity in db.AbstractAssignmentActivities
                         where activity.AbstractAssignmentID == assignmentId
                         && activity.TeamUsers.Count > 0
                         select activity.TeamUsers).FirstOrDefault();
            var narrowedQuery = baseQuery.Where(q => q is UserMember).Select(q => q as UserMember);
            if (narrowedQuery.Count() > 0)
            {
                return narrowedQuery.AsQueryable();
            }

            //this must be a team assignment, get users from the teams
            var listOfUsers = (from teamUsers in GetAssignmentTeams(assignmentId)
                                          join teams in db.Teams on teamUsers.TeamID equals teams.ID
                                          select teams.Members).ToList();
            List<UserMember> users = new List<UserMember>();
            foreach (ICollection<TeamUserMember> userList in listOfUsers)
            {
                foreach (TeamUserMember userMember in userList)
                {
                    users.Add(userMember as UserMember);
                }
            }
            return users.AsQueryable();
        }

        public IQueryable<TeamUserMember> GetTeamUsersForAssignment(int assignmentId)
        {
            var query = (from activity in db.AbstractAssignmentActivities
                        where activity.AbstractAssignmentID == assignmentId
                        && activity.TeamUsers.Count > 0
                        select activity.TeamUsers).FirstOrDefault();
            return query.AsQueryable();
        }


        public void AddTeamUserMember(TeamUserMember tum)
        {

        }
    }
}


