using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace JaahdLogistics.Models
{
    public class PurchaseOrder
    {
        public int Id { get; set; }
        public string PONumber { get; set; } = string.Empty;
        public int PRId { get; set; }
        public int ProjectId { get; set; }
        public int? BidAnalysisId { get; set; }
        public int? VendorId { get; set; }
        public DateTime Date { get; set; }
        public string? Terms { get; set; }
        public string Status { get; set; } = "Pending";
        public ObservableCollection<POItem> Items { get; set; } = new();

        public byte[]? LogisticsSignature { get; set; }
        public byte[]? FinanceSignature { get; set; }
        public byte[]? PMSignature { get; set; }
        public byte[]? FinalSignature { get; set; }

        public string? LogisticsName { get; set; }
        public string? FinanceName { get; set; }
        public string? PMName { get; set; }
        public string? FinalName { get; set; }

        public decimal TotalAmount => Items.Sum(i => i.TotalPrice);
    }

    public class POItem : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
    {
        public int Id { get; set; }
        public int POId { get; set; }

        private string _description = string.Empty;
        public string Description { get => _description; set => SetProperty(ref _description, value); }

        private string? _unit;
        public string? Unit { get => _unit; set => SetProperty(ref _unit, value); }

        private decimal _quantity;
        public decimal Quantity { get => _quantity; set { SetProperty(ref _quantity, value); OnPropertyChanged(nameof(TotalPrice)); } }

        private decimal _unitPrice;
        public decimal UnitPrice { get => _unitPrice; set { SetProperty(ref _unitPrice, value); OnPropertyChanged(nameof(TotalPrice)); } }

        public decimal TotalPrice => Quantity * UnitPrice;
    }
}
