using System.Data;
using System.Data.SqlClient;

namespace SqlData.Core
{
    public static class SqlConstraints
    {
        public static void DisableAllConstraints(SqlConnection sqlConnection)
        {
            using (var command = sqlConnection.CreateCommand())
            {
                command.CommandType = CommandType.Text;
                command.CommandText = "EXEC sp_MSForEachTable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL';";

                command.ExecuteNonQuery();
            }
        }

        public static void EnableAllConstraints(SqlConnection sqlConnection)
        {
            using (var command = sqlConnection.CreateCommand())
            {
                command.CommandType = CommandType.Text;
                command.CommandText = "EXEC sp_MSForEachTable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL';";

                command.ExecuteNonQuery();
            }
        }
    }
}