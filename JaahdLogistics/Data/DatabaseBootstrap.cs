using System;
using System.IO;
using Microsoft.Data.Sqlite;
using Dapper;

namespace JaahdLogistics.Data
{
    public class DatabaseBootstrap
    {
        private readonly string _connectionString;

        public DatabaseBootstrap(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void Setup()
        {
            using var connection = new SqliteConnection(_connectionString);

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string schemaPath = Path.Combine(baseDir, "schema.sql");

            if (File.Exists(schemaPath))
            {
                string schema = File.ReadAllText(schemaPath);
                connection.Execute(schema);
            }

            var userCount = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Users");
            if (userCount == 0)
            {
                string adminHash = JaahdLogistics.Helpers.SecurityHelper.HashPassword("admin");
                string pmHash = JaahdLogistics.Helpers.SecurityHelper.HashPassword("pm");
                string procHash = JaahdLogistics.Helpers.SecurityHelper.HashPassword("proc");
                string finHash = JaahdLogistics.Helpers.SecurityHelper.HashPassword("fin");
                string storeHash = JaahdLogistics.Helpers.SecurityHelper.HashPassword("store");
                string headHash = JaahdLogistics.Helpers.SecurityHelper.HashPassword("head");

                connection.Execute("INSERT INTO Users (Username, PasswordHash, Role, FullName) VALUES ('admin', @adminHash, 'Admin', 'System Administrator')", new { adminHash });
                connection.Execute("INSERT INTO Users (Username, PasswordHash, Role, FullName) VALUES ('pm', @pmHash, 'ProjectManager', 'Project Manager')", new { pmHash });
                connection.Execute("INSERT INTO Users (Username, PasswordHash, Role, FullName) VALUES ('proc', @procHash, 'ProcurementManager', 'Procurement Manager')", new { procHash });
                connection.Execute("INSERT INTO Users (Username, PasswordHash, Role, FullName) VALUES ('fin', @finHash, 'FinanceManager', 'Finance Manager')", new { finHash });
                connection.Execute("INSERT INTO Users (Username, PasswordHash, Role, FullName) VALUES ('store', @storeHash, 'Storekeeper', 'Storekeeper')", new { storeHash });
                connection.Execute("INSERT INTO Users (Username, PasswordHash, Role, FullName) VALUES ('head', @headHash, 'HeadOfAssociation', 'Head of Association')", new { headHash });
            }

            var settingsCount = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Settings WHERE Id = 1");
            if (settingsCount == 0)
            {
                connection.Execute("INSERT INTO Settings (Id, AssociationName) VALUES (1, 'Jaahd Association')");
            }
        }
    }
}
