using System;
using Microsoft.Data.Sqlite;
using Dapper;

namespace JaahdLogistics.Helpers
{
    public class NumberingHelper
    {
        private readonly string _connectionString;

        public NumberingHelper(string connectionString)
        {
            _connectionString = connectionString;
        }

        public string GenerateNumber(string prefix, int projectId)
        {
            using var connection = new SqliteConnection(_connectionString);
            var project = connection.QuerySingleOrDefault<JaahdLogistics.Models.Project>("SELECT * FROM Projects WHERE Id = @projectId", new { projectId });
            if (project == null) return $"{prefix}-{DateTime.Now.Year}-001";
            
            string projectCode = project.Code;
            int year = project.Year;
            
            // Get current count for this type and project
            string countQuery = prefix switch {
                "PR" => "SELECT COUNT(*) FROM PurchaseRequisitions WHERE ProjectId = @projectId",
                "PO" => "SELECT COUNT(*) FROM PurchaseOrders po JOIN PurchaseRequisitions pr ON po.PRId = pr.Id WHERE pr.ProjectId = @projectId",
                "RFQ" => "SELECT COUNT(*) FROM RFQs r JOIN PurchaseRequisitions pr ON r.PRId = pr.Id WHERE pr.ProjectId = @projectId",
                "GRN" => "SELECT COUNT(*) FROM GoodsReceivingNotes g JOIN PurchaseOrders po ON g.POId = po.Id JOIN PurchaseRequisitions pr ON po.PRId = pr.Id WHERE pr.ProjectId = @projectId",
                _ => "SELECT 0"
            };

            // SQLite COUNT(*) returns Int64 (long)
            var count = connection.ExecuteScalar<long>(countQuery, new { projectId });
            
            return $"{projectCode}-{year}-JAAHD-{prefix}-{(count + 1):D3}";
        }
    }
}
