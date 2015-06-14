using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

using Dapper;
using OSBLEPlus.Logic.DomainObjects.Interfaces;
using OSBLEPlus.Logic.Utility;

namespace OSBLEPlus.Logic.DataAccess.Activities
{
    public class Posts
    {
        private const int BatchSize = 100;

        public static bool Post(IEnumerable<IActivityEvent> events)
        {
            try
            {
                var sql = new StringBuilder();
                var activityEvents = events as IActivityEvent[] ?? events.ToArray();
                var batches = activityEvents.Length / BatchSize;

                for (var b = 0; b < batches + 1; b++)
                {
                    sql.Clear();
                    sql.AppendFormat("DECLARE {0} INT{1}", StringConstants.SqlHelperScopeIdentityName, Environment.NewLine);

                    var from = b * BatchSize;
                    var to = (b + 1) * BatchSize > activityEvents.Length ? activityEvents.Length : (b + 1) * BatchSize;

                    for (var idx = from; idx < to; idx++)
                    {
                        var eventLog = activityEvents[idx];
                        sql.AppendFormat("{0}{1}", eventLog.GetInsertScripts(), Environment.NewLine);
                    }

                    //execute sql batch insert statements
                    using (var connection = new SqlConnection(StringConstants.ConnectionString))
                    {
                        connection.Execute(sql.ToString());
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                //TODO: inject Log4Net to log error details into files
                return false;
            }
        }
    }
}
