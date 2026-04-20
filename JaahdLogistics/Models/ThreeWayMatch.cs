using System;

namespace JaahdLogistics.Models
{
    public class ThreeWayMatch
    {
        public int Id { get; set; }
        public int POId { get; set; }
        public int GRNId { get; set; }
        public string? InvoiceNumber { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
        public string Status { get; set; } = "Pending";
    }
}
