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

            var adminExists = connection.ExecuteScalar<bool>("SELECT COUNT(1) FROM Users WHERE Username = 'admin'");
            if (!adminExists)
            {
                connection.Execute("INSERT INTO Users (Username, PasswordHash, Role, FullName) VALUES ('admin', 'admin', 'Admin', 'System Administrator')");
            }

            var settingsExists = connection.ExecuteScalar<bool>("SELECT COUNT(1) FROM Settings WHERE Id = 1");
            if (!settingsExists)
            {
                connection.Execute("INSERT INTO Settings (Id, AssociationName) VALUES (1, 'Jaahd Association')");
            }
        }
    }
}
