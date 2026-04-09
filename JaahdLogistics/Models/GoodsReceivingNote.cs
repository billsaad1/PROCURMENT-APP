using System;
using System.Collections.Generic;

namespace JaahdLogistics.Models
{
    public class GoodsReceivingNotes
    {
        public int Id { get; set; }
        public string GRNNumber { get; set; } = string.Empty;
        public int POId { get; set; }
        public DateTime Date { get; set; }
        public int ReceiverId { get; set; }
        public string Status { get; set; } = "Completed";
    }

    public class GRNItems
    {
        public int Id { get; set; }
        public int GRNId { get; set; }
        public int POItemId { get; set; }
        public decimal ReceivedQuantity { get; set; }
        public decimal AcceptedQuantity { get; set; }
        public decimal RejectedQuantity { get; set; }
        public string? RejectReason { get; set; }
    }
}
