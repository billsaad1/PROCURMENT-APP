using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using Dapper;

namespace JaahdLogistics.Services
{
    public class ReportService
    {
        private readonly string _connectionString;

        public ReportService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public dynamic GetSpendingPerProject()
        {
            using var connection = new SqliteConnection(_connectionString);
            return connection.Query(
                "SELECT p.Name, SUM(pi.Quantity * pi.UnitPrice) as TotalSpent " +
                "FROM Projects p " +
                "JOIN PurchaseRequisitions pr ON p.Id = pr.ProjectId " +
                "JOIN PRItems pi ON pr.Id = pi.PRId " +
                "WHERE pr.Status = 'FinalApproved' " +
                "GROUP BY p.Name");
        }

        public dynamic GetInventoryStatus()
        {
            using var connection = new SqliteConnection(_connectionString);
            return connection.Query(
                "SELECT i.ItemDescription, p.Name as ProjectName, i.CurrentQuantity " +
                "FROM Inventory i " +
                "JOIN Projects p ON i.ProjectId = p.Id");
        }
    }
}
