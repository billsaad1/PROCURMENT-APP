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

                // Repair Projects table
                try { connection.Execute("ALTER TABLE Projects ADD COLUMN Name TEXT NOT NULL DEFAULT ''"); } catch { }
                try { connection.Execute("ALTER TABLE Projects ADD COLUMN Code TEXT NOT NULL DEFAULT ''"); } catch { }
                try { connection.Execute("ALTER TABLE Projects ADD COLUMN Year INTEGER NOT NULL DEFAULT 0"); } catch { }

                // Repair BudgetLines table
                try { connection.Execute("ALTER TABLE BudgetLines ADD COLUMN Name TEXT NOT NULL DEFAULT ''"); } catch { }
                try { connection.Execute("ALTER TABLE BudgetLines ADD COLUMN Unit TEXT"); } catch { }
                try { connection.Execute("ALTER TABLE BudgetLines ADD COLUMN Quantity DECIMAL(18, 2) NOT NULL DEFAULT 0"); } catch { }
                try { connection.Execute("ALTER TABLE BudgetLines ADD COLUMN UnitPrice DECIMAL(18, 2) NOT NULL DEFAULT 0"); } catch { }
                try { connection.Execute("ALTER TABLE BudgetLines ADD COLUMN Currency TEXT NOT NULL DEFAULT 'USD'"); } catch { }

                // Repair PurchaseRequisitions table
                try { connection.Execute("ALTER TABLE PurchaseRequisitions ADD COLUMN Currency TEXT NOT NULL DEFAULT 'USD'"); } catch { }
                try { connection.Execute("ALTER TABLE PurchaseRequisitions ADD COLUMN ExchangeRate DECIMAL(18, 4) DEFAULT 1.0"); } catch { }

                // Repair Settings table
                try { connection.Execute("ALTER TABLE Settings ADD COLUMN Address TEXT"); } catch { }
                try { connection.Execute("ALTER TABLE Settings ADD COLUMN ContactInfo TEXT"); } catch { }
                try { connection.Execute("ALTER TABLE Settings ADD COLUMN PRTerms TEXT"); } catch { }
                try { connection.Execute("ALTER TABLE Settings ADD COLUMN RFQTerms TEXT"); } catch { }
                try { connection.Execute("ALTER TABLE Settings ADD COLUMN POTerms TEXT"); } catch { }

                // Add signature blob to Users if missing (though it should be there)
                try { connection.Execute("ALTER TABLE Users ADD COLUMN SignatureImage BLOB"); } catch { }

                // Repair ThreeWayMatch table
                try { connection.Execute("ALTER TABLE ThreeWayMatch ADD COLUMN InvoiceDetails TEXT"); } catch { }
                try { connection.Execute("ALTER TABLE ThreeWayMatch ADD COLUMN InvoiceScan BLOB"); } catch { }

                // Repair PurchaseOrders table
                try { connection.Execute("ALTER TABLE PurchaseOrders ADD COLUMN ProjectId INTEGER NOT NULL DEFAULT 0"); } catch { }
            }

            var userCount = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Users");
            if (userCount == 0)
            {
                CreateUser(connection, "admin", "admin", "Admin", "System Administrator");
                CreateUser(connection, "pm", "pm", "ProjectManager", "Project Manager");
                CreateUser(connection, "log", "log", "LogisticsManager", "Logistics Manager");
                CreateUser(connection, "proc", "proc", "ProcurementManager", "Procurement Manager");
                CreateUser(connection, "fin", "fin", "FinanceManager", "Finance Manager");
                CreateUser(connection, "store", "store", "Storekeeper", "Storekeeper");
                CreateUser(connection, "head", "head", "HeadOfAssociation", "Head of Association");
            }
            else
            {
                // Repair step: Ensure all default users exist and have hashed passwords
                UpdateOrResetUser(connection, "admin", "admin", "Admin", "System Administrator");
                UpdateOrResetUser(connection, "pm", "pm", "ProjectManager", "Project Manager");
                UpdateOrResetUser(connection, "log", "log", "LogisticsManager", "Logistics Manager");
                UpdateOrResetUser(connection, "proc", "proc", "ProcurementManager", "Procurement Manager");
                UpdateOrResetUser(connection, "fin", "fin", "FinanceManager", "Finance Manager");
                UpdateOrResetUser(connection, "store", "store", "Storekeeper", "Storekeeper");
                UpdateOrResetUser(connection, "head", "head", "HeadOfAssociation", "Head of Association");
            }

            var settingsCount = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Settings WHERE Id = 1");
            if (settingsCount == 0)
            {
                connection.Execute("INSERT INTO Settings (Id, AssociationName) VALUES (1, 'Jaahd Association')");
            }
        }

        private void CreateUser(SqliteConnection connection, string username, string password, string role, string fullName)
        {
            string hash = JaahdLogistics.Helpers.SecurityHelper.HashPassword(password);
            connection.Execute("INSERT INTO Users (Username, PasswordHash, Role, FullName) VALUES (@username, @hash, @role, @fullName)",
                new { username, hash, role, fullName });
        }

        private void UpdateOrResetUser(SqliteConnection connection, string username, string password, string role, string fullName)
        {
            var user = connection.QuerySingleOrDefault<JaahdLogistics.Models.User>("SELECT * FROM Users WHERE Username = @username", new { username });
            string hash = JaahdLogistics.Helpers.SecurityHelper.HashPassword(password);

            if (user == null)
            {
                CreateUser(connection, username, password, role, fullName);
            }
            else if (user.PasswordHash == password) // Reset if it's plaintext
            {
                connection.Execute("UPDATE Users SET PasswordHash = @hash, Role = @role, FullName = @fullName WHERE Username = @username",
                    new { hash, username, role, fullName });
            }
        }
    }
}
