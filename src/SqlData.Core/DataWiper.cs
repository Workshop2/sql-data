using System.Data;
using System.Data.SqlClient;
using SqlData.Core.CommonSql;

namespace SqlData.Core
{
    public class DataWiper
    {
        private readonly string _connectionString;

        public DataWiper(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void Execute()
        {
            using (var sqlConnection = new SqlConnector().Connect(_connectionString))
            {
                SqlConstraints.DisableAllConstraints(sqlConnection);

                using (var command = sqlConnection.CreateCommand())
                {
                    command.CommandType = CommandType.Text;
                    command.CommandText = @"Exec sp_MSForEachTable
                                            '
                                            If ObjectProperty(Object_ID(''?''), ''TableHasForeignRef'') = 1
                                            Begin
                                                -- Just to know what all table used delete syntax.
                                                Print ''Delete from '' + ''?''
                                                Delete From ?
                                            End
                                            Else
                                            Begin
                                                -- Just to know what all table used Truncate syntax.
                                                Print ''Truncate Table '' + ''?''
                                                Truncate Table ?
                                            End
                                            '";

                    command.ExecuteNonQuery();
                }

                SqlConstraints.EnableAllConstraints(sqlConnection);
            }
        }

        public void Execute(string tableName)
        {
            using (var connection = new SqlConnector().Connect(_connectionString))
            {
                Execute(connection, tableName);
            }
        }

        /// <summary>
        /// Expects constraints to be disabled already
        /// </summary>
        public void Execute(SqlConnection connection, string tableName)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandType = CommandType.Text;
                const string sql = @"
                                            If ObjectProperty(Object_ID('{0}'), 'TableHasForeignRef') = 1
                                            Begin
                                                -- Just to know what all table used delete syntax.
                                                Print 'Delete from ' + '{0}'
                                                Delete From {0}
                                            End
                                            Else
                                            Begin
                                                -- Just to know what all table used Truncate syntax.
                                                Print 'Truncate Table ' + '{0}'
                                                Truncate Table {0}
                                            End
                                        ";

                command.CommandText = string.Format(sql, tableName);

                command.ExecuteNonQuery();
            }
        }
    }
}