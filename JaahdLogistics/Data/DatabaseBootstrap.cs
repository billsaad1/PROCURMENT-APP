using System;
using System.IO;
using System.Linq;
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
            connection.Open();
            connection.Execute("PRAGMA foreign_keys = ON;");
            
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string schemaPath = Path.Combine(baseDir, "schema.sql");
            
            if (File.Exists(schemaPath))
            {
                string schema = File.ReadAllText(schemaPath);
                connection.Execute(schema);
                
                // Repair Projects table
                AddColumnIfMissing(connection, "Projects", "Name", "TEXT NOT NULL DEFAULT ''");
                AddColumnIfMissing(connection, "Projects", "Code", "TEXT NOT NULL DEFAULT ''");
                AddColumnIfMissing(connection, "Projects", "Year", "INTEGER NOT NULL DEFAULT 0");

                // Repair BudgetLines table
                AddColumnIfMissing(connection, "BudgetLines", "Name", "TEXT NOT NULL DEFAULT ''");
                AddColumnIfMissing(connection, "BudgetLines", "Unit", "TEXT");
                AddColumnIfMissing(connection, "BudgetLines", "Quantity", "DECIMAL(18, 2) NOT NULL DEFAULT 0");
                AddColumnIfMissing(connection, "BudgetLines", "UnitPrice", "DECIMAL(18, 2) NOT NULL DEFAULT 0");
                AddColumnIfMissing(connection, "BudgetLines", "Currency", "TEXT NOT NULL DEFAULT 'USD'");

                // Repair PurchaseRequisitions table
                AddColumnIfMissing(connection, "PurchaseRequisitions", "Currency", "TEXT NOT NULL DEFAULT 'USD'");
                AddColumnIfMissing(connection, "PurchaseRequisitions", "ExchangeRate", "DECIMAL(18, 4) DEFAULT 1.0");
                
                // Repair Settings table
                AddColumnIfMissing(connection, "Settings", "Address", "TEXT");
                AddColumnIfMissing(connection, "Settings", "ContactInfo", "TEXT");
                AddColumnIfMissing(connection, "Settings", "Tel", "TEXT");
                AddColumnIfMissing(connection, "Settings", "Email", "TEXT");
                AddColumnIfMissing(connection, "Settings", "PRTerms", "TEXT");
                AddColumnIfMissing(connection, "Settings", "RFQTerms", "TEXT");
                AddColumnIfMissing(connection, "Settings", "POTerms", "TEXT");

                // Repair Users
                AddColumnIfMissing(connection, "Users", "SignatureImage", "BLOB");

                // Ensure Vendors table exists
                connection.Execute(@"
                    CREATE TABLE IF NOT EXISTS Vendors (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL,
                        Address TEXT,
                        Contact TEXT,
                        Tel TEXT,
                        Email TEXT,
                        Category TEXT,
                        TaxId TEXT,
                        BankInfo TEXT,
                        IsActive INTEGER DEFAULT 1
                    )");

                // Repair ThreeWayMatch table
                AddColumnIfMissing(connection, "ThreeWayMatch", "InvoiceDetails", "TEXT");
                AddColumnIfMissing(connection, "ThreeWayMatch", "InvoiceScan", "BLOB");

                // Repair PurchaseOrders table
                AddColumnIfMissing(connection, "PurchaseOrders", "ProjectId", "INTEGER NOT NULL DEFAULT 0");

                // Repair BidAnalyses table
                AddColumnIfMissing(connection, "BidAnalyses", "Currency", "TEXT");
                AddColumnIfMissing(connection, "BidAnalyses", "ExchangeRate", "DECIMAL(18, 4)");
                AddColumnIfMissing(connection, "BidAnalyses", "RecommendationReasons", "TEXT");

                // Repair BidItems table
                AddColumnIfMissing(connection, "BidItems", "BudgetLineId", "INTEGER");

                // Repair Bidders table
                AddColumnIfMissing(connection, "Bidders", "VendorId", "INTEGER");
                AddColumnIfMissing(connection, "Bidders", "Tel", "TEXT");
                AddColumnIfMissing(connection, "Bidders", "Email", "TEXT");
                AddColumnIfMissing(connection, "Bidders", "Justification", "TEXT");
                AddColumnIfMissing(connection, "Bidders", "IsWinner", "INTEGER DEFAULT 0");
                AddColumnIfMissing(connection, "Bidders", "Discount", "DECIMAL(18, 2) DEFAULT 0");
                AddColumnIfMissing(connection, "Bidders", "MiscCosts", "DECIMAL(18, 2) DEFAULT 0");
                AddColumnIfMissing(connection, "Bidders", "TotalAmount", "DECIMAL(18, 2) DEFAULT 0");
                AddColumnIfMissing(connection, "Bidders", "QuoteScan", "BLOB");

                // Repair PurchaseOrders table
                AddColumnIfMissing(connection, "PurchaseOrders", "ProjectId", "INTEGER NOT NULL DEFAULT 0");
                AddColumnIfMissing(connection, "PurchaseOrders", "BidderId", "INTEGER");
                AddColumnIfMissing(connection, "PurchaseOrders", "VendorId", "INTEGER");
                AddColumnIfMissing(connection, "PurchaseOrders", "Clause", "TEXT");
                AddColumnIfMissing(connection, "PurchaseOrders", "Currency", "TEXT");
                AddColumnIfMissing(connection, "PurchaseOrders", "ExchangeRate", "DECIMAL(18, 4)");
                AddColumnIfMissing(connection, "PurchaseOrders", "VendorName", "TEXT");
                AddColumnIfMissing(connection, "PurchaseOrders", "VendorContact", "TEXT");
                AddColumnIfMissing(connection, "PurchaseOrders", "VendorTel", "TEXT");
                AddColumnIfMissing(connection, "PurchaseOrders", "VendorEmail", "TEXT");
                AddColumnIfMissing(connection, "PurchaseOrders", "VendorAddress", "TEXT");

                // Repair POItems table
                AddColumnIfMissing(connection, "POItems", "BudgetLineId", "INTEGER");
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

            // Cleanup invalid Foreign Keys that might cause crashes in existing data
            CleanupInvalidForeignKeys(connection);
        }

        private void CleanupInvalidForeignKeys(SqliteConnection connection)
        {
            try
            {
                // Find a default Project and User for repair
                var defaultProjectId = connection.ExecuteScalar<int?>("SELECT Id FROM Projects LIMIT 1");
                var defaultUserId = connection.ExecuteScalar<int?>("SELECT Id FROM Users LIMIT 1");

                if (defaultProjectId.HasValue)
                {
                    connection.Execute("UPDATE PurchaseRequisitions SET ProjectId = @pid WHERE ProjectId = 0 OR ProjectId NOT IN (SELECT Id FROM Projects)", new { pid = defaultProjectId.Value });
                    connection.Execute("UPDATE PurchaseOrders SET ProjectId = @pid WHERE ProjectId = 0 OR ProjectId NOT IN (SELECT Id FROM Projects)", new { pid = defaultProjectId.Value });
                }

                if (defaultUserId.HasValue)
                {
                    connection.Execute("UPDATE PurchaseRequisitions SET RequesterId = @uid WHERE RequesterId = 0 OR RequesterId NOT IN (SELECT Id FROM Users)", new { uid = defaultUserId.Value });
                }

                // Set invalid BidAnalysisId to NULL
                connection.Execute("UPDATE PurchaseOrders SET BidAnalysisId = NULL WHERE BidAnalysisId IS NOT NULL AND (BidAnalysisId = 0 OR BidAnalysisId NOT IN (SELECT Id FROM BidAnalyses))");
                // Set invalid BidderId to NULL
                connection.Execute("UPDATE PurchaseOrders SET BidderId = NULL WHERE BidderId IS NOT NULL AND (BidderId = 0 OR BidderId NOT IN (SELECT Id FROM Bidders))");
                // Set invalid VendorId to NULL
                connection.Execute("UPDATE PurchaseOrders SET VendorId = NULL WHERE VendorId IS NOT NULL AND (VendorId = 0 OR VendorId NOT IN (SELECT Id FROM Vendors))");

                // Repair items
                connection.Execute("UPDATE PRItems SET BudgetLineId = NULL WHERE BudgetLineId IS NOT NULL AND (BudgetLineId = 0 OR BudgetLineId NOT IN (SELECT Id FROM BudgetLines))");
                connection.Execute("UPDATE POItems SET BudgetLineId = NULL WHERE BudgetLineId IS NOT NULL AND (BudgetLineId = 0 OR BudgetLineId NOT IN (SELECT Id FROM BudgetLines))");
                connection.Execute("UPDATE BidItems SET BudgetLineId = NULL WHERE BudgetLineId IS NOT NULL AND (BudgetLineId = 0 OR BudgetLineId NOT IN (SELECT Id FROM BudgetLines))");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cleaning up FKs: {ex.Message}");
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
            else if (user.PasswordHash == password || !JaahdLogistics.Helpers.SecurityHelper.VerifyPassword(password, user.PasswordHash)) 
            {
                // Reset if it's plaintext OR if verification fails (might happen if salt changed or hash was corrupted)
                connection.Execute("UPDATE Users SET PasswordHash = @hash, Role = @role, FullName = @fullName WHERE Username = @username", 
                    new { hash, username, role, fullName });
            }
        }

        private void AddColumnIfMissing(SqliteConnection connection, string tableName, string columnName, string columnDefinition)
        {
            bool exists = false;
            try
            {
                // PRAGMA table_info returns rows, where 'name' is one of the columns.
                // We use dynamic to handle the multi-column result set.
                var tableInfo = connection.Query($"PRAGMA table_info({tableName})");
                foreach (var row in tableInfo)
                {
                    // Dapper dynamic rows for SQLite might have properties named 'name'
                    if (row.name != null && row.name.ToString().Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    connection.Execute($"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking/adding column {columnName} to {tableName}: {ex.Message}");
            }
        }
    }
}
