using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using CommunityToolkit.Mvvm.ComponentModel;

namespace JaahdLogistics.Models
{
    public partial class PurchaseOrder : ObservableObject
    {
        public int Id { get; set; }
        
        [ObservableProperty]
        private string _pONumber = string.Empty;
        
        [ObservableProperty]
        private int _pRId;

        [ObservableProperty]
        private int _projectId;

        [ObservableProperty]
        private int? _bidAnalysisId;

        [ObservableProperty]
        private int? _bidderId;

        [ObservableProperty]
        private int? _vendorId;
        
        [ObservableProperty]
        private DateTime _date;
        
        [ObservableProperty]
        private string? _terms;

        [ObservableProperty]
        private string? _clause;
        
        [ObservableProperty]
        private Vendor? _vendor;

        [ObservableProperty]
        private string _status = "Pending";

        [ObservableProperty]
        private string? _currency;

        [ObservableProperty]
        private decimal _exchangeRate = 1.0m;
        
        public ObservableCollection<POItem> Items { get; set; } = new();

        [ObservableProperty]
        private byte[]? _logisticsSignature;

        [ObservableProperty]
        private byte[]? _financeSignature;

        [ObservableProperty]
        private byte[]? _pmSignature;

        [ObservableProperty]
        private byte[]? _finalSignature;

        [ObservableProperty]
        private string? _logisticsName;

        [ObservableProperty]
        private string? _financeName;

        [ObservableProperty]
        private string? _pmName;

        [ObservableProperty]
        private string? _finalName;

        public decimal TotalAmount => Items.Sum(i => i.TotalPrice);

        public PurchaseOrder()
        {
            Date = DateTime.Now;
            Status = "Pending";
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
