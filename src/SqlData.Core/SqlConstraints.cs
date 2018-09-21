using System;
using System.Data.SqlClient;
using Dapper;
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
                    sqlConnection.Execute("EXEC sp_MSForEachTable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL';");
                });
        }

        public static void EnableAllConstraints(SqlConnection sqlConnection)
        {
            Policy
                .Handle<SqlException>()
                .WaitAndRetry(new[] { TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10) })
                .Execute(() =>
                {
                    sqlConnection.Execute("EXEC sp_MSForEachTable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL';");
                });
        }
    }
}