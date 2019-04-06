using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Data;
using System.IO;
using System.Text;

namespace Beste.Rights
{
    public static class AccessControlListAdapter
    {
        private static StringCollection ValidateTable(DataTable table)
        {
            var problems = new CommaDelimitedStringCollection();

            if (!table.Columns.Contains("Principal"))
                problems.Add( "Missing Column: Principal" );

            if (!table.Columns.Contains("Operation"))
                problems.Add(  "Missing Column: Operation" );

            if (!table.Columns.Contains("Resource"))
                problems.Add(  "Missing Column: Resource" );

            for (var i = 0; i < table.Rows.Count; i++)
            {
                if (table.Rows[i].IsNull("Principal"))
                    problems.Add(string.Format("Missing Principal on Row {0:n0}", i + 1));

                if (table.Rows[i].IsNull("Operation"))
                    problems.Add(string.Format("Missing Operation on Row {0:n0}", i + 1));

                if (table.Rows[i].IsNull("Resource"))
                    problems.Add(string.Format("Missing Resource on Row {0:n0}", i + 1));
            }

            return problems;
        }

        public static void Load(IAccessControlList acl, string grantedFilePath, string deniedFilePath)
        {
            var granted = LoadCsvIntoTable(grantedFilePath);
            var denied = LoadCsvIntoTable(deniedFilePath);
            Load(acl,granted,denied);
        }

        /// <summary>
        /// Assumes UTF8 encoding.
        /// </summary>
        private static DataTable LoadCsvIntoTable(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                var csv = new CsvHelper();
                csv.Read(stream, Encoding.UTF8);
                return csv.Table;
            }
        }

        public static void Load(IAccessControlList acl, DataTable granted, DataTable denied)
        {
            var problems = ValidateTable(granted);
            if ( problems.Count > 0 )
                throw new Exception("Invalid Access Granted Table: " + problems);

            problems = ValidateTable(denied);
            if (problems.Count > 0)
                throw new Exception("Invalid Access Denied Table: " + problems);

            for (var i = 0; i < granted.Rows.Count; i++)
            {
                var row = granted.Rows[i];
                
                var principal = (string) row["Principal"];
                var operation = (string)row["Operation"];
                var resource = (string)row["Resource"];
                
                acl.Grant(principal,operation,resource);
            }

            for (var i = 0; i < denied.Rows.Count; i++)
            {
                var row = denied.Rows[i];

                var principal = (string)row["Principal"];
                var operation = (string)row["Operation"];
                var resource = (string)row["Resource"];

                acl.Deny(principal, operation, resource);
            }
        }
    }
}