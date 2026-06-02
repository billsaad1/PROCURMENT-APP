using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace JaahdLogistics.Models
{
    public class PurchaseOrder : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
    {
        public int Id { get; set; }
        
        private string _poNumber = string.Empty;
        public string PONumber { get => _poNumber; set => SetProperty(ref _poNumber, value); }
        
        public int PRId { get; set; }
        public int ProjectId { get; set; } 
        public int? BidAnalysisId { get; set; }
        public int? VendorId { get; set; }
        
        private DateTime _date;
        public DateTime Date { get => _date; set => SetProperty(ref _date, value); }
        
        private string? _terms;
        public string? Terms { get => _terms; set => SetProperty(ref _terms, value); }

        private string? _clause;
        public string? Clause { get => _clause; set => SetProperty(ref _clause, value); }
        
        private Bidder? _vendor;
        public Bidder? Vendor { get => _vendor; set => SetProperty(ref _vendor, value); }

        private string _status = "Pending";
        public string Status { get => _status; set => SetProperty(ref _status, value); }
        
        public ObservableCollection<POItem> Items { get; set; } = new();

        private byte[]? _logisticsSignature;
        public byte[]? LogisticsSignature { get => _logisticsSignature; set => SetProperty(ref _logisticsSignature, value); }

        private byte[]? _financeSignature;
        public byte[]? FinanceSignature { get => _financeSignature; set => SetProperty(ref _financeSignature, value); }

        private byte[]? _pmSignature;
        public byte[]? PMSignature { get => _pmSignature; set => SetProperty(ref _pmSignature, value); }

        private byte[]? _finalSignature;
        public byte[]? FinalSignature { get => _finalSignature; set => SetProperty(ref _finalSignature, value); }

        private string? _logisticsName;
        public string? LogisticsName { get => _logisticsName; set => SetProperty(ref _logisticsName, value); }

        private string? _financeName;
        public string? FinanceName { get => _financeName; set => SetProperty(ref _financeName, value); }

        private string? _pmName;
        public string? PMName { get => _pmName; set => SetProperty(ref _pmName, value); }

        private string? _finalName;
        public string? FinalName { get => _finalName; set => SetProperty(ref _finalName, value); }

        public decimal TotalAmount => Items.Sum(i => i.TotalPrice);

        public PurchaseOrder()
        {
            Items.CollectionChanged += (s, e) => {
                OnPropertyChanged(nameof(TotalAmount));
                if (e.NewItems != null) {
                    foreach (POItem item in e.NewItems) item.PropertyChanged += (s2, e2) => OnPropertyChanged(nameof(TotalAmount));
                }
            };
        }
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
