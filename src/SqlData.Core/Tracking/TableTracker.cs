using System;
using System.Data.SqlClient;
using System.IO;
using SqlData.Core.CommonSql;
using System.Linq;
using System.Threading.Tasks;
using Dapper;

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
            var now = GetSnapshot();
            var changedTables = _latestSnapshot
                                            .Where(x => x.Value != now[x.Key])
                                            .Select(x => x.Key)
                                            .ToList();

            if (!changedTables.Any())
            {
                return;
            }

            var dataWiper = new DataWiper(_connectionString);
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                SqlConstraints.DisableAllConstraints(connection);

                var tasks = changedTables
                    .Select(x => dataWiper.ExecuteAsync(_connectionString, x));

                Task.WhenAll(tasks)
                    .GetAwaiter()
                    .GetResult();
                
                var toSql = new DataToSql(_connectionString, _directory, changedTables);
                toSql.Execute();

                SqlConstraints.EnableAllConstraints(connection);
            }

            TakeSnapshot();
        }

        private Snapshot GetSnapshot()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var checksums = connection
                    .Query<TableChecksumDao>(Sql.ChecksumForAllTables());

                var result = new Snapshot();
                foreach (var tableChecksum in checksums)
                {
                    result[tableChecksum.TableName] = tableChecksum.Checksum;
                }

                return result;
            }
        }
    }
}