using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OSBLE.Models.Triggers
{
    public class DiscussionTeamDeleteTrigger : ModelTrigger
    {
        protected override string TriggerString
        {
            get
            {
                string queryString = "CREATE TRIGGER [dbo].[DiscussionTeamDelete]\n"
                                    + " ON [dbo].[DiscussionTeams]\n"
                                    + " INSTEAD OF DELETE\n"
                                    + " AS\n"
                                    + " BEGIN;\n"
                                    + "     DELETE FROM DiscussionPosts WHERE DiscussionTeamId IN (SELECT ID FROM DELETED);\n"
                                    + "     DELETE FROM DiscussionTeams WHERE ID IN (SELECT ID FROM DELETED);\n"
                                    + " END;";
                return queryString;
            }
        }
    }
}
