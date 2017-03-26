using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;

namespace SqlData.Core
{
    public class DataToSql
    {
        private readonly string _connectionString;
        private readonly string _directory;
        private readonly List<string> _tables;

        public DataToSql(string connectionString, string directory)
            : this(connectionString, directory, new List<string>())
        {

        }

        public DataToSql(string connectionString, string directory, IEnumerable<string> tables)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new NullReferenceException("connctionString");

            if (string.IsNullOrEmpty(directory))
                throw new NullReferenceException("directory");

            if (!Directory.Exists(directory))
                throw new DirectoryNotFoundException(directory);

            _connectionString = connectionString;

            _directory = Path.GetFullPath(directory);

            _tables = tables.Select(x => x.Replace("[", string.Empty).Replace("]", string.Empty)).ToList();
        }

        public void Execute()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                SqlConstraints.DisableAllConstraints(connection);
                Execute(connection);
                SqlConstraints.EnableAllConstraints(connection);
            }
        }

        public void Execute(SqlConnection connection)
        {
            using (var sqlBulkCopy = new SqlBulkCopy(_connectionString, SqlBulkCopyOptions.KeepIdentity))
            {
                foreach (var dataFile in Directory.GetFiles(_directory, "*.data"))
                {
                    string tableName = Path.GetFileNameWithoutExtension(dataFile);

                    if (_tables.Any() && !_tables.Any(x => x.Equals(tableName)))
                    {
                        continue;
                    }

                    using (var dataSet = new DataSet())
                    {
                        dataSet.ReadXml(dataFile);

                        if (dataSet.Tables.Count > 0)
                        {
                            // should only need to execute table 0
                            sqlBulkCopy.DestinationTableName = CommonSql.Sql.GetSafeTableName(tableName);
                            sqlBulkCopy.WriteToServer(dataSet.Tables[0]);
                        }
                    }
                }
            }
        }
    }
}