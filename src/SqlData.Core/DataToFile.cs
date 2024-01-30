using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using Dapper;
using Microsoft.Data.SqlClient;
using SqlData.Core.CommonSql;

namespace SqlData.Core
{
    public class DataToFile
    {
        private readonly string _connectionString;
        private readonly string _targetDirectory;

        public DataToFile(string connectionString, string targetDirectory)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new NullReferenceException("connctionString");

            if (string.IsNullOrEmpty(targetDirectory))
                throw new NullReferenceException("targetDirectory");

            if (!Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            _connectionString = connectionString;
            _targetDirectory = Path.GetFullPath(targetDirectory);
        }

        public void Execute()
        {
            ClearOldFiles(_targetDirectory);

            using (var sqlConnection = new SqlConnector().Connect(_connectionString))
            {
                foreach (var table in Tables(sqlConnection))
                {
                    var file = Path.Combine(_targetDirectory, table + ".data");

                    using (var sqlDataAdapter = new SqlDataAdapter())
                    {
                        using (var command = sqlConnection.CreateCommand())
                        {
                            command.CommandText = $"SELECT * FROM {Sql.GetSafeTableName(table)};";
                            sqlDataAdapter.SelectCommand = command;

                            using (var dataSet = new DataSet())
                            {
                                sqlDataAdapter.Fill(dataSet);

                                foreach (DataTable dataSetTable in dataSet.Tables)
                                {
                                    foreach (DataColumn column in dataSetTable.Columns)
                                    {
                                        if (column.DataType == typeof(DateTime))
                                        {
                                            column.DateTimeMode = DataSetDateTime.Unspecified;
                                        }
                                    }
                                }

                                using (var fileStream = File.Create(file))
                                {
                                    dataSet.WriteXml(fileStream, XmlWriteMode.WriteSchema);
                                }
                            }
                        }
                    }
                }
            }
        }
        
        private static void ClearOldFiles(string directory)
        {
            foreach (var dataFile in Directory.GetFiles(directory, "*.data"))
            {
                File.Delete(dataFile);
            }
        }

        private IEnumerable<string> Tables(SqlConnection sqlConnection)
        {
            return sqlConnection.Query<string>("SELECT [TABLE_SCHEMA]  + '.' + [TABLE_NAME] FROM information_schema.tables WHERE [TABLE_NAME] <> 'sysdiagrams' AND [TABLE_TYPE] <> 'VIEW';");
        }
    }
}