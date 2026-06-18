using System;
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
                "SELECT v.Name as VendorName, poi.Quantity, poi.UnitPrice, po.Currency, po.ExchangeRate " +
                "FROM Vendors v " +
                "JOIN PurchaseOrders po ON v.Id = po.VendorId " +
                "JOIN POItems poi ON po.Id = poi.POId " +
                "WHERE po.Status = 'FinalApproved' OR po.Status = 'ApprovedByHead'");

            return results.GroupBy(r => (string)r.VendorName).Select(g => new {
                VendorName = g.Key,
                OrderCount = g.Count(),
                TotalValue = g.Sum(r => {
                    decimal q = Convert.ToDecimal(r.Quantity);
                    decimal p = Convert.ToDecimal(r.UnitPrice);
                    decimal rate = Convert.ToDecimal(r.ExchangeRate ?? 1.0);
                    decimal total = q * p;
                    if (r.Currency == "YER" && rate > 0) return total / rate;
                    return total;
                })
            }).OrderByDescending(x => x.TotalValue).ToList();
        }

        public dynamic GetProcurementPipeline()
        {
            using var connection = new SqliteConnection(_connectionString);
            var prs = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM PurchaseRequisitions");
            var rfqs = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM RFQs");
            var bas = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM BidAnalyses");
            var pos = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM PurchaseOrders");
            var grns = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM GoodsReceivingNotes");

            return new List<dynamic>
            {
                new { Stage = "Purchase Requisitions", Count = prs, Icon = "📄" },
                new { Stage = "Requests for Quotation", Count = rfqs, Icon = "✉️" },
                new { Stage = "Bid Analyses", Count = bas, Icon = "📊" },
                new { Stage = "Purchase Orders", Count = pos, Icon = "💰" },
                new { Stage = "Goods Receiving Notes", Count = grns, Icon = "📦" }
            };
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
