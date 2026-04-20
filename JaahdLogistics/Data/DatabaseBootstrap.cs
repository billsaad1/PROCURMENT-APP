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
                connection.Execute("INSERT INTO Users (Username, PasswordHash, Role, FullName) VALUES ('admin', 'admin', 'Admin', 'System Administrator')");
                connection.Execute("INSERT INTO Users (Username, PasswordHash, Role, FullName) VALUES ('pm', 'pm', 'ProjectManager', 'Project Manager')");
                connection.Execute("INSERT INTO Users (Username, PasswordHash, Role, FullName) VALUES ('proc', 'proc', 'ProcurementManager', 'Procurement Manager')");
                connection.Execute("INSERT INTO Users (Username, PasswordHash, Role, FullName) VALUES ('fin', 'fin', 'FinanceManager', 'Finance Manager')");
                connection.Execute("INSERT INTO Users (Username, PasswordHash, Role, FullName) VALUES ('store', 'store', 'Storekeeper', 'Storekeeper')");
                connection.Execute("INSERT INTO Users (Username, PasswordHash, Role, FullName) VALUES ('head', 'head', 'HeadOfAssociation', 'Head of Association')");
            }

            var settingsCount = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Settings WHERE Id = 1");
            if (settingsCount == 0)
            {
                connection.Execute("INSERT INTO Settings (Id, AssociationName) VALUES (1, 'Jaahd Association')");
            }
        }
    }
}
