using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Data.SqlClient;
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
                    try
                    {
                        UpdateTable(sqlBulkCopy, dataFile);
                    }
                    catch (Exception ex)
                    {
                        throw new TableFailedToPopulate(dataFile, ex);
                    }
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
                dataSet = ReadTableFromDisk(dataFile);
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

        private static DataSet ReadTableFromDisk(string dataFile)
        {
            var xmlFile = XDocument.Load(dataFile);
            FixCrossPlatformGuidAssemblyReferences(xmlFile);

            var dataSet = new DataSet();
            dataSet.ReadXml(xmlFile.CreateReader());
            return dataSet;
        }

        private static void FixCrossPlatformGuidAssemblyReferences(XContainer xmlFile)
        {
            XNamespace xs = "http://www.w3.org/2001/XMLSchema";
            var columnSchemas = xmlFile
                .Descendants(xs + "element")
                .ToList();

            var guidType = typeof(Guid);
            var guidAssemblyName = guidType.AssemblyQualifiedName;

            XNamespace msdata = "urn:schemas-microsoft-com:xml-msdata";
            foreach (var element in columnSchemas)
            {
                var attributes = element.Attributes(msdata + "DataType");
                foreach (var attribute in attributes)
                {
                    if (attribute.Value.Contains("System.Guid"))
                    {
                        attribute.Value = guidAssemblyName;
                    }
                }
            }
        }
    }
}