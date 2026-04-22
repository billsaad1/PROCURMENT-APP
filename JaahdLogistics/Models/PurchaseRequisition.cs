using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace JaahdLogistics.Models
{
    public class PurchaseRequisition
    {
        public int Id { get; set; }
        public string PRNumber { get; set; } = string.Empty;
        public int ProjectId { get; set; }
        public int RequesterId { get; set; }
        public DateTime Date { get; set; }
        public string? Justification { get; set; }
        public string Currency { get; set; } = "USD";
        public decimal ExchangeRate { get; set; } = 1.0m;
        public string Status { get; set; } = "Pending";
        
        public ObservableCollection<PRItem> Items { get; set; } = new();
    }

    public class PRItem
    {
        public int Id { get; set; }
        public int PRId { get; set; }
        public int BudgetLineId { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? Unit { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice => Quantity * UnitPrice;
    }
}
