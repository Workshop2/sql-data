using System;
using System.Data;
using System.Data.SqlClient;
using Polly;

namespace SqlData.Core
{
    public static class SqlConstraints
    {
        public static void DisableAllConstraints(SqlConnection sqlConnection)
        {
            Policy
                .Handle<SqlException>()
                .WaitAndRetry(new[] { TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10) })
                .Execute(() =>
                {
                    using (var command = sqlConnection.CreateCommand())
                    {
                        command.CommandType = CommandType.Text;
                        command.CommandText = "EXEC sp_MSForEachTable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL';";

                        command.ExecuteNonQuery();
                    }
                });
        }

        public static void EnableAllConstraints(SqlConnection sqlConnection)
        {
            Policy
                .Handle<SqlException>()
                .WaitAndRetry(new[] { TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10) })
                .Execute(() =>
                {
                    using (var command = sqlConnection.CreateCommand())
                    {
                        command.CommandType = CommandType.Text;
                        command.CommandText = "EXEC sp_MSForEachTable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL';";

                        command.ExecuteNonQuery();
                    }
                });
        }
    }
}