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
                "WHERE pr.Status = 'FinalApproved'");

            // Group by project in memory to handle currency conversion accurately
            return items.GroupBy(i => i.ProjectName).Select(g => new {
                Name = g.Key,
                TotalSpent = g.Sum(i => {
                    decimal itemTotal = (decimal)i.Quantity * (decimal)i.UnitPrice;
                    if (i.Currency == "YER" && (decimal)i.ExchangeRate > 0) return itemTotal / (decimal)i.ExchangeRate;
                    return itemTotal;
                })
            }).ToList();
        }

        public dynamic GetVendorHistory()
        {
            using var connection = new SqliteConnection(_connectionString);
            return connection.Query(
                "SELECT b.Name as VendorName, COUNT(po.Id) as OrderCount, SUM(poi.Quantity * poi.UnitPrice) as TotalValue " +
                "FROM Bidders b " +
                "JOIN PurchaseOrders po ON b.Id = po.VendorId " +
                "JOIN POItems poi ON po.Id = poi.POId " +
                "WHERE po.Status = 'FinalApproved' " +
                "GROUP BY b.Name");
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
