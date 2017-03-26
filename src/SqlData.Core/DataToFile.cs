using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using PetaPoco;

namespace SqlData.Core
{
    public class DataToFile
    {
        private readonly string _connectionString;
        private readonly string _directory;

        public DataToFile(string connectionString, string directory)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new NullReferenceException("connctionString");

            if (string.IsNullOrEmpty(directory))
                throw new NullReferenceException("directory");

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            _connectionString = connectionString;
            _directory = Path.GetFullPath(directory);
        }

        public void Execute()
        {
            ClearOldFiles(_directory);

            using (var sqlConnection = new SqlConnection(_connectionString))
            {
                sqlConnection.Open();

                foreach (var table in Tables())
                {
                    string file = Path.Combine(_directory, table + ".data");

                    using (var sqlDataAdapter = new SqlDataAdapter())
                    {
                        using (var command = sqlConnection.CreateCommand())
                        {
                            command.CommandText = string.Format("SELECT * FROM {0};", CommonSql.Sql.GetSafeTableName(table));
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
                                    //using (var zipStream = new GZipStream(fileStream, CompressionMode.Compress))
                                    //{
                                    dataSet.WriteXml(fileStream, XmlWriteMode.WriteSchema);
                                    //}
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

        private IEnumerable<string> Tables()
        {
            using (var database = Database.GetDatabase(_connectionString))
            {
                foreach (var table in database.Query<string>("SELECT [TABLE_SCHEMA]  + '.' + [TABLE_NAME] FROM information_schema.tables WHERE [TABLE_NAME] <> 'sysdiagrams';"))
                {
                    yield return table;
                }
            }
        }
    }
}