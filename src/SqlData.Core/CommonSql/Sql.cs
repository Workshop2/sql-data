using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace SqlData.Core.CommonSql
{
    public static class Sql
    {
        const string NAMESPACE = "SqlData.Core.CommonSql.Scripts.";

        private static Assembly _assembly;
        private static Assembly Assembly
        {
            get { return _assembly ?? (_assembly = Assembly.GetExecutingAssembly()); }
        }

        private static Dictionary<string, string> _scriptCache = new Dictionary<string, string>();
        private static string GetScript(string scriptName)
        {
            if (!_scriptCache.ContainsKey(scriptName))
            {
                using (Stream scriptStream = Assembly.GetManifestResourceStream(NAMESPACE + scriptName))
                {
                    using (StreamReader reader = new StreamReader(scriptStream))
                    {
                        _scriptCache[scriptName] = reader.ReadToEnd();
                    }
                }
            }

            return _scriptCache[scriptName];
        }

        public static string ChecksumForAllTables()
        {
            return GetScript("ChecksumForAllTables.sql");
        }
        
        public static string GetSafeTableName(string table)
        {
            string[] split = (table ?? string.Empty).Replace("[", "").Replace("]", "").Split('.');

            string result = string.Empty;
            foreach (var part in split)
            {
                if (!string.IsNullOrEmpty(part))
                {
                    if (!string.IsNullOrEmpty(result))
                    {
                        result += ".";
                    }
                    result += string.Format("[{0}]", part);
                }
            }

            return result;
        }
    }
}