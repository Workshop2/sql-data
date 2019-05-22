using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
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
        public void Execute(SqlConnection sqlConnection, string tableName)
        {
            sqlConnection.Execute(string.Format(WipeTableSql, tableName));
        }

        /// <summary>
        /// Execute against many tables, excluding specified.
        /// </summary>
        public void Execute(SqlConnection sqlConnection, IEnumerable<string> tablesToExclude)
        {
            SqlConstraints.DisableAllConstraints(sqlConnection);
            var tablesToExecuteAgainst = TablesToWipe(tablesToExclude);
            
            foreach (var tableName in tablesToExecuteAgainst)
            {
                Execute(sqlConnection, tableName);
            }

            SqlConstraints.EnableAllConstraints(sqlConnection);
        }

        private IEnumerable<string> TablesToWipe(IEnumerable<string> tablesToExclude)
        {
            var allTables = GetAllTables(_connectionString);

            var allTablesNames = allTables.Select(a => $"[{a.TABLE_SCHEMA}].[{a.TABLE_NAME}]");

            return allTablesNames.Except(tablesToExclude);
        }

        private static IEnumerable<DatabaseTableDto> GetAllTables(string connectionString)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                return connection.Query<DatabaseTableDto>(@"
                    SELECT TABLE_SCHEMA, TABLE_NAME
                    FROM INFORMATION_SCHEMA.TABLES
                    WHERE TABLE_TYPE = 'BASE TABLE'");
            }
        }

        private class DatabaseTableDto
        {
            public  string TABLE_SCHEMA { get; set; }
            public string TABLE_NAME { get; set; }
        }

        /// <summary>
        /// Expects constraints to be disabled already
        /// </summary>
        public async Task ExecuteAsync(string connectionString, string tableName)
        {
            using (var sqlConnection = new SqlConnection(connectionString))
            {
                await sqlConnection.OpenAsync();
                await sqlConnection.ExecuteAsync(string.Format(WipeTableSql, tableName));
            }
        }
    }
}