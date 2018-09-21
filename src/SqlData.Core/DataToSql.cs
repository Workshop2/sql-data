using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SqlData.Core.CommonSql;

namespace SqlData.Core
{
    public class DataToSql
    {
        private readonly string _connectionString;
        private readonly string _directory;
        private readonly List<string> _tables;
        private static Dictionary<string, DataSet> ScriptCache = new Dictionary<string, DataSet>();

        public DataToSql(string connectionString, string directory)
            : this(connectionString, directory, new List<string>())
        { }

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

        public void DisableConstraintsAndExecute()
        {
            using (var connection = new SqlConnector().Connect(_connectionString))
            {
                SqlConstraints.DisableAllConstraints(connection);
                Execute();
                SqlConstraints.EnableAllConstraints(connection);
            }
        }

        public void Execute()
        {
            using (var sqlBulkCopy = new SqlBulkCopy(_connectionString, SqlBulkCopyOptions.KeepIdentity))
            {
                foreach (var dataFile in Directory.GetFiles(_directory, "*.data"))
                {
                    UpdateTable(sqlBulkCopy, dataFile);
                }
            }
        }

        private void UpdateTable(SqlBulkCopy sqlBulkCopy, string dataFile)
        {
            var tableName = Path.GetFileNameWithoutExtension(dataFile);

            if (_tables.Any() && !_tables.Any(x => x.Equals(tableName)))
            {
                return;
            }

            DataSet dataSet;
            if (ScriptCache.ContainsKey(tableName))
            {
                dataSet = ScriptCache[tableName];
            }
            else
            {
                dataSet = new DataSet();
                dataSet.ReadXml(dataFile);
                ScriptCache[tableName] = dataSet;
            }

            if (dataSet.Tables.Count <= 0)
            {
                return;
            }

            // should only need to execute table 0
            sqlBulkCopy.DestinationTableName = Sql.GetSafeTableName(tableName);
            sqlBulkCopy.WriteToServer(dataSet.Tables[0]);
        }
    }
}