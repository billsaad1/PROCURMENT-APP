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
            int year = project.Year;

            // Get current count for this type and project
            string table = prefix switch {
                "PR" => "PurchaseRequisitions",
                "PO" => "PurchaseOrders",
                "RFQ" => "RFQs",
                "GRN" => "GoodsReceivingNotes",
                _ => "PurchaseRequisitions"
            };

            var count = connection.ExecuteScalar<int>($"SELECT COUNT(*) FROM {table} WHERE ProjectId = @projectId", new { projectId });

            return $"{prefix}-{projectCode}-{year}-{(count + 1):D3}";
        }
    }
}
