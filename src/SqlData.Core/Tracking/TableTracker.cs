using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using SqlData.Core.CommonSql;
using System.Linq;

namespace SqlData.Core.Tracking
{
    public class TableTracker
    {
        private readonly string _connectionString;
        private readonly string _directory;
        private Snapshot _latestSnapshot;

        public TableTracker(string connectionString, string directory)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new NullReferenceException("connctionString");

            if (string.IsNullOrEmpty(directory))
                throw new NullReferenceException("directory");

            if (!Directory.Exists(directory))
                throw new DirectoryNotFoundException(directory);

            _connectionString = connectionString;
            _directory = directory;
        }

        public void TakeSnapshot()
        {
            _latestSnapshot = GetSnapshot();
        }

        public void RevertToSnapshot()
        {
            Snapshot now = GetSnapshot();

            List<string> changedTables = _latestSnapshot.Where(x => x.Value != now[x.Key]).Select(x => x.Key).ToList();

            if (!changedTables.Any())
            {
                return;
            }

            DataWiper dataWiper = new DataWiper(_connectionString);
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                SqlConstraints.DisableAllConstraints(connection);

                foreach (string tableName in changedTables)
                {
                    dataWiper.Execute(connection, tableName);
                }

                DataToSql toSql = new DataToSql(_connectionString, _directory, changedTables);
                toSql.Execute(connection);

                SqlConstraints.EnableAllConstraints(connection);
            }

            TakeSnapshot();
        }

        private Snapshot GetSnapshot()
        {
            Snapshot result = new Snapshot();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = Sql.ChecksumForAllTables();
                    command.CommandType = CommandType.Text;

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        int tableNameOrdinal = reader.GetOrdinal("TableName");
                        int checksumOrdinal = reader.GetOrdinal("Checksum");

                        while (reader.Read())
                        {
                            if (reader.IsDBNull(tableNameOrdinal))
                            {
                                continue;
                            }

                            int checkSum = reader.IsDBNull(checksumOrdinal) ? 0 : reader.GetInt32(checksumOrdinal);

                            result[reader.GetString(tableNameOrdinal)] = checkSum;
                        }
                    }
                }
            }

            return result;
        }
    }
}