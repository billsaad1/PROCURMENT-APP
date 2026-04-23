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
            var items = connection.Query(
                "SELECT p.Name as ProjectName, bl.Code as BudgetLine, pi.Quantity, pi.UnitPrice, pr.Currency, pr.ExchangeRate " +
                "FROM Projects p " +
                "JOIN BudgetLines bl ON p.Id = bl.ProjectId " +
                "JOIN PRItems pi ON bl.Id = pi.BudgetLineId " +
                "JOIN PurchaseRequisitions pr ON pi.PRId = pr.Id " +
                "WHERE pr.Status != 'Rejected'");

            // Group by project in memory to handle currency conversion accurately
            return items.GroupBy(i => i.ProjectName).Select(g => new {
                Name = g.Key,
                TotalSpent = g.Sum(i => {
                    decimal quantity = Convert.ToDecimal(i.Quantity);
                    decimal unitPrice = Convert.ToDecimal(i.UnitPrice);
                    decimal exchangeRate = Convert.ToDecimal(i.ExchangeRate ?? 1.0);
                    decimal itemTotal = quantity * unitPrice;

                    // Standardize to USD for report comparison
                    if (i.Currency == "YER" && exchangeRate > 0) return itemTotal / exchangeRate;
                    return itemTotal;
                })
            }).ToList();
        }

        public dynamic GetVendorHistory()
        {
            using var connection = new SqliteConnection(_connectionString);
            var results = connection.Query(
                "SELECT b.Name as VendorName, poi.Quantity, poi.UnitPrice " +
                "FROM Bidders b " +
                "JOIN PurchaseOrders po ON b.Id = po.VendorId " +
                "JOIN POItems poi ON po.Id = poi.POId " +
                "WHERE po.Status = 'FinalApproved'");

            return results.GroupBy(r => r.VendorName).Select(g => new {
                VendorName = g.Key,
                OrderCount = g.Count(), // This is actually item count, but simpler for demo
                TotalValue = g.Sum(r => Convert.ToDecimal(r.Quantity) * Convert.ToDecimal(r.UnitPrice))
            }).ToList();
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
