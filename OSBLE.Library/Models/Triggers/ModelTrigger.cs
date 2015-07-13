using System;
using System.Data.Entity.Infrastructure;
using System.Data.EntityClient;
using System.Data.SqlClient;

namespace OSBLE.Models.Triggers
{
    public abstract class ModelTrigger
    {
        protected abstract string TriggerString { get; }

        public bool CreateTrigger(ContextBase db)
        {
            var context = (db as IObjectContextAdapter).ObjectContext;
            var entityConnection = context.Connection as EntityConnection;
            var dbConn = entityConnection.StoreConnection as SqlConnection;
            try
            {
                dbConn.Open();
            }
            catch(Exception)
            {
                dbConn.Close();
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
                dbConn.Close();
                return false;
            }

            dbConn.Close();
            return true;
        }
    }
}
