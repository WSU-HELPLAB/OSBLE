using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OSBLE.Models.Triggers
{
    class NotificationDeleteTrigger : ModelTrigger
    {
        protected override string TriggerString
        {
            get
            {
                string queryString = "CREATE TRIGGER [dbo].[NotificationDelete]\n"
                                    + " ON [dbo].[Notifications]\n"
                                    + " INSTEAD OF DELETE\n"
                                    + " AS\n"
                                    + " BEGIN;\n"
                                    + "     DELETE FROM Notifications WHERE RecipientID IN (SELECT ID FROM DELETED);\n"
                                    + "     DELETE FROM Notifications WHERE SenderID IN (SELECT ID FROM DELETED);\n"
                                    + " END;";
                return queryString;
            }
        }
    }
}
