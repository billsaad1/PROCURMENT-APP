using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace JaahdLogistics.Models
{
    public partial class BidAnalysis : ObservableObject
    {
        public int Id { get; set; }
        public int RFQId { get; set; }
        
        [ObservableProperty]
        private DateTime _date = DateTime.Now;
        
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(RecommendedBidderName))]
        [NotifyPropertyChangedFor(nameof(RecommendedBidderTotal))]
        private int? _recommendedBidderId;
        
        [ObservableProperty]
        private string? _justification;

        [ObservableProperty]
        private string? _recommendationReasons;
        
        [ObservableProperty]
        private string _status = "Pending";
        
        [ObservableProperty]
        private string? _currency;
        
        [ObservableProperty]
        private decimal _exchangeRate = 1.0m;
        
        public ObservableCollection<Bidder> Bidders { get; set; } = new();

        public string RecommendedBidderName => Bidders.FirstOrDefault(b => b.Id == RecommendedBidderId)?.Name ?? "None";
        public decimal RecommendedBidderTotal => Bidders.FirstOrDefault(b => b.Id == RecommendedBidderId)?.CalculatedTotal ?? 0;
    }

    public partial class Bidder : ObservableObject
    {
        public int Id { get; set; }
        public int BidAnalysisId { get; set; }
        
        [ObservableProperty]
        private int? _vendorId;

        [ObservableProperty]
        private string _name = string.Empty;
        
        [ObservableProperty]
        private string? _address;

        [ObservableProperty]
        private string? _contact;

        [ObservableProperty]
        private string? _tel;

        [ObservableProperty]
        private string? _email;

        [ObservableProperty]
        private string? _justification;

        [ObservableProperty]
        private bool _isWinner;
        
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CalculatedTotal))]
        private decimal _discount;
        
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CalculatedTotal))]
        private decimal _miscCosts;
        
        public decimal TotalAmount { get; set; }

        [ObservableProperty]
        private byte[]? _quoteScan;
        public ObservableCollection<BidItem> Items { get; set; } = new();

        public decimal CalculatedSubTotal => Items.Sum(i => i.TotalPrice);
        public decimal CalculatedTotal => CalculatedSubTotal - Discount + MiscCosts;

        public Bidder()
        {
            Items.CollectionChanged += (s, e) => {
                OnPropertyChanged(nameof(CalculatedSubTotal));
                OnPropertyChanged(nameof(CalculatedTotal));
                if (e.NewItems != null) {
                    foreach (BidItem item in e.NewItems) item.PropertyChanged += (s2, e2) => {
                        OnPropertyChanged(nameof(CalculatedSubTotal));
                        OnPropertyChanged(nameof(CalculatedTotal));
                    };
                }
            };
        }
    }

    public class BidItem : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
    {
        public int Id { get; set; }
        public int BidderId { get; set; }
        public int? BudgetLineId { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? Unit { get; set; }
        public decimal Quantity { get; set; }
        private decimal _unitPrice;
        public decimal UnitPrice 
        { 
            get => _unitPrice; 
            set { SetProperty(ref _unitPrice, value); OnPropertyChanged(nameof(TotalPrice)); } 
        }
        public decimal TotalPrice => Quantity * UnitPrice;
    }
}
