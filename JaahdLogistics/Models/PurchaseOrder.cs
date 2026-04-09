using System;
using System.Collections.Generic;

namespace JaahdLogistics.Models
{
    public class PurchaseOrder
    {
        public int Id { get; set; }
        public string PONumber { get; set; } = string.Empty;
        public int PRId { get; set; }
        public int ProjectId { get; set; } // Added ProjectId
        public int BidAnalysisId { get; set; }
        public int VendorId { get; set; }
        public DateTime Date { get; set; }
        public string? Terms { get; set; }
        public string Status { get; set; } = "Pending";
        public List<POItem> Items { get; set; } = new();
    }

    public class POItem
    {
        public int Id { get; set; }
        public int POId { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? Unit { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice => Quantity * UnitPrice;
    }
}
