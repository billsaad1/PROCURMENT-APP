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
            var project = connection.QuerySingle<dynamic>("SELECT Code, Year FROM Projects WHERE Id = @projectId", new { projectId });

            string projectCode = project.Code;
            int year = (int)(long)project.Year;

            // Get current count for this type and project
            // Note: Since RFQ and PO are linked to PR, they inherit the project ID.
            string countQuery = prefix switch {
                "PR" => "SELECT COUNT(*) FROM PurchaseRequisitions WHERE ProjectId = @projectId",
                "PO" => "SELECT COUNT(*) FROM PurchaseOrders po JOIN PurchaseRequisitions pr ON po.PRId = pr.Id WHERE pr.ProjectId = @projectId",
                "RFQ" => "SELECT COUNT(*) FROM RFQs r JOIN PurchaseRequisitions pr ON r.PRId = pr.Id WHERE pr.ProjectId = @projectId",
                "GRN" => "SELECT COUNT(*) FROM GoodsReceivingNotes g JOIN PurchaseOrders po ON g.POId = po.Id JOIN PurchaseRequisitions pr ON po.PRId = pr.Id WHERE pr.ProjectId = @projectId",
                _ => "SELECT 0"
            };

            var count = connection.ExecuteScalar<int>(countQuery, new { projectId });

            // Requested Format: [ProjectCode]-[Year]-JAAHD-[Sequence]
            // For example: EDU-2024-JAAHD-001
            // Adding prefix (PR/PO) to differentiate document types within the same sequence or format
            return $"{projectCode}-{year}-JAAHD-{prefix}-{(count + 1):D3}";
        }
    }
}
