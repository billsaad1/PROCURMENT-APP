using System;
using System.Collections.Generic;
using System.Linq;

namespace JaahdLogistics.Models
{
    public class ThreeWayMatch : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
    {
        public int Id { get; set; }
        public int POId { get; set; }
        public int GRNId { get; set; }
        public string? TWMNumber { get; set; }
        public string? InvoiceNumber { get; set; }
        public string? InvoiceDetails { get; set; }
        public byte[]? InvoiceScan { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
        public string Status { get; set; } = "Pending";

        public List<ThreeWayMatchItem> Items { get; set; } = new();
    }

    public class ThreeWayMatchItem : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
    {
        public int Index { get; set; }
        public int Id { get; set; }
        public int ThreeWayMatchId { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? Unit { get; set; }

        // PO Values
        public decimal POPrice { get; set; }
        public decimal POQuantity { get; set; }

        // Extract Values (Manual Entry)
        private decimal _extractPrice;
        public decimal ExtractPrice { get => _extractPrice; set { SetProperty(ref _extractPrice, value); OnPropertyChanged(nameof(HasDiff)); } }

        private decimal _extractQuantity;
        public decimal ExtractQuantity { get => _extractQuantity; set { SetProperty(ref _extractQuantity, value); OnPropertyChanged(nameof(HasDiff)); } }

        // GRN Values
        public decimal GRNPrice { get; set; }
        public decimal GRNQuantity { get; set; }

        public bool HasDiff => POPrice != ExtractPrice || POPrice != GRNPrice || POQuantity != ExtractQuantity || POQuantity != GRNQuantity;
        public string DiffNote => HasDiff ? "YES" : "NO";
        public string? ClarifyDiff { get; set; }
    }
}
