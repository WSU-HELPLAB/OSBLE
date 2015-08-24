using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OSBLE.Models.Triggers
{
    public class CourseUserDeleteTrigger : ModelTrigger
    {
        protected override string TriggerString
        {
            get
            {
                string queryString = "CREATE TRIGGER [dbo].[CourseUserDelete]\n"
                                    + " ON [dbo].[CourseUsers]\n"
                                    + " INSTEAD OF DELETE\n"
                                    + " AS\n"
                                    + " BEGIN;\n"
                                    + "     DELETE FROM AbstractDashboards WHERE CourseUserID IN (SELECT ID FROM DELETED);\n"
                                    + "     DELETE FROM DiscussionAssignmentMetaInfoes WHERE CourseUserID IN (SELECT ID FROM DELETED);\n"
                                    + "     DELETE FROM DiscussionPosts WHERE CourseUserID IN (SELECT ID FROM DELETED);\n"
                                    + "     DELETE FROM Events WHERE PosterID IN (SELECT ID FROM DELETED);\n"
                                    + "     DELETE FROM Notifications WHERE SenderID IN (SELECT ID FROM DELETED);\n"
                                    + "     DELETE FROM Notifications WHERE RecipientID IN (SELECT ID FROM DELETED);\n"
                                    + "     DELETE FROM RubricEvaluations WHERE EvaluatorID IN (SELECT ID FROM DELETED);\n"
                                    + "     DELETE FROM TeamEvaluations WHERE EvaluatorID IN (SELECT ID FROM DELETED);\n"
                                    + "     DELETE FROM TeamEvaluations WHERE RecipientID IN (SELECT ID FROM DELETED);\n"
                                    + "     DELETE FROM TeamMembers WHERE CourseUserID IN (SELECT ID FROM DELETED);\n"
                                    + "     DELETE FROM CourseUsers WHERE ID IN (SELECT ID FROM DELETED);\n"
                                    + " END;";
                return queryString;
            }
        }
    }
}
