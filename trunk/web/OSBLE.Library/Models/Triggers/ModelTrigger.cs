using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Objects;
using System.Data.Entity.Infrastructure;
using System.Data.EntityClient;
using System.Data.Common;
using System.Data.SqlClient;

namespace OSBLE.Models.Triggers
{
    public abstract class ModelTrigger
    {
        protected abstract string TriggerString { get; }

        public bool CreateTrigger(ContextBase db)
        {
            ObjectContext context = (db as IObjectContextAdapter).ObjectContext;
            var entityConnection = context.Connection as EntityConnection;
            SqlConnection dbConn = entityConnection.StoreConnection as SqlConnection;
            try
            {
                dbConn.Open();
            }
            catch(Exception)
            {
                return false;
            }

            string query = TriggerString;
            SqlCommand cmd = new SqlCommand(query, dbConn);
            try
            {
                object result = cmd.ExecuteNonQuery();
            }
            catch(Exception)
            {
                return false;
            }
            return true;
        }
    }
}
