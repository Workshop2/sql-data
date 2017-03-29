using System;
using System.Data.SqlClient;
using Polly;

namespace SqlData.Core.CommonSql
{
    public class SqlConnector
    {
        public SqlConnection Connect(string connectionString)
        {
            return Policy
                 .Handle<SqlException>(exception => exception.Message.Contains("No process is on the other end of the pipe"))
                 .WaitAndRetry(new[] { TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10) })
                 .Execute(() =>
                 {
                     SqlConnection connection = null;
                     try
                     {
                         connection = new SqlConnection(connectionString);
                         connection.Open();
                     }
                     catch
                     {
                         connection?.Dispose();
                         throw;
                     }

                     return connection;
                 });
        }
    }
}