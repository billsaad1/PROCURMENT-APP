using System;
using System.Collections.Generic;

namespace JaahdLogistics.Models
{
    public class BidAnalysis
    {
        public int Id { get; set; }
        public int RFQId { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
        public int? RecommendedBidderId { get; set; }
        public string? Justification { get; set; }
        public string Status { get; set; } = "Pending";
        public List<Bidder> Bidders { get; set; } = new();
    }

    public class Bidder
    {
        public int Id { get; set; }
        public int BidAnalysisId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Contact { get; set; }
        public List<BidItem> Items { get; set; } = new();
    }

    public class BidItem
    {
        public int Id { get; set; }
        public int BidderId { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? Unit { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice => Quantity * UnitPrice;
    }
}
