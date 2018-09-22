using System;
using System.Data.SqlClient;
using System.Diagnostics;
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
            var stopWatch = Stopwatch.StartNew();
            _latestSnapshot = GetSnapshot();
            Console.WriteLine($"TakeSnapshot {stopWatch.ElapsedMilliseconds} Milliseconds");
        }

        public void RevertToSnapshot()
        {
            //var stopWatch = Stopwatch.StartNew();
            var now = GetSnapshot();
            //Console.WriteLine($"Get snapshot {stopWatch.ElapsedMilliseconds} Milliseconds");

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

                //stopWatch.Restart();
                SqlConstraints.DisableAllConstraints(connection);
                //Console.WriteLine($"DisableAllConstraints {stopWatch.ElapsedMilliseconds} Milliseconds");

                //stopWatch.Restart();
                foreach (var changedTable in changedTables)
                {
                    dataWiper.Execute(connection, changedTable);
                }
                //Console.WriteLine($"dataWiper {stopWatch.ElapsedMilliseconds} Milliseconds");

                //stopWatch.Restart();
                new DataToSql(_connectionString, _directory, changedTables)
                    .Execute();
                //Console.WriteLine($"DataToSql {stopWatch.ElapsedMilliseconds} Milliseconds");

                //stopWatch.Restart();
                SqlConstraints.EnableAllConstraintsQuick(connection);
                //Console.WriteLine($"EnableAllConstraints {stopWatch.ElapsedMilliseconds} Milliseconds");
            }
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