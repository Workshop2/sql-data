using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
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

        private const string WipeTableSql = @"
                                            If ObjectProperty(Object_ID('{0}'), 'TableHasForeignRef') = 1
                                            Begin
                                                Delete From {0}
                                            End
                                            Else
                                            Begin
                                                Truncate Table {0}
                                            End
                                        ";

        /// <summary>
        /// Expects constraints to be disabled already
        /// </summary>
        public async Task ExecuteAsync(SqlConnection connection, string tableName)
        {
            await connection.ExecuteAsync(string.Format(WipeTableSql, tableName));
        }
    }
}