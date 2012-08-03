using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OSBLE.Models.Triggers
{
    public class TeamDeleteTrigger : ModelTrigger
    {
        protected override string TriggerString
        {
            get
            {
                string queryString = "CREATE TRIGGER [dbo].[TeamDelete]\n"
                                    + " ON [dbo].[Teams]\n" 
                                    + " INSTEAD OF DELETE\n"
                                    + " AS\n"
                                    + " BEGIN;\n"
	                                + "     DELETE FROM AssignmentTeams WHERE TeamID IN (SELECT ID FROM DELETED);\n"
	                                + "     DELETE FROM DiscussionTeams WHERE TeamID IN (SELECT ID FROM DELETED);\n"
	                                + "     DELETE FROM DiscussionTeams WHERE AuthorTeamID IN (SELECT ID FROM DELETED);\n"
	                                + "     DELETE FROM ReviewTeams WHERE AuthorTeamID IN (SELECT ID FROM DELETED);\n"
	                                + "     DELETE FROM ReviewTeams WHERE ReviewTeamID IN (SELECT ID FROM DELETED);\n"
	                                + "     DELETE FROM RubricEvaluations WHERE RecipientID IN (SELECT ID FROM DELETED);\n"
	                                + "     DELETE FROM TeamMembers WHERE TeamID IN (SELECT ID FROM DELETED);\n"
                                    + " END;";
                return queryString;
            }
        }
    }
}
