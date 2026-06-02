using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace JaahdLogistics.Models
{
    public class BidAnalysis : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
    {
        public int Id { get; set; }
        public int RFQId { get; set; }
        
        private DateTime _date = DateTime.Now;
        public DateTime Date { get => _date; set => SetProperty(ref _date, value); }
        
        private int? _recommendedBidderId;
        public int? RecommendedBidderId { get => _recommendedBidderId; set { if (SetProperty(ref _recommendedBidderId, value)) OnPropertyChanged(nameof(RecommendedBidderName)); } }
        
        private string? _justification;
        public string? Justification { get => _justification; set => SetProperty(ref _justification, value); }
        
        private string _status = "Pending";
        public string Status { get => _status; set => SetProperty(ref _status, value); }
        
        private string? _currency;
        public string? Currency { get => _currency; set => SetProperty(ref _currency, value); }
        
        private decimal _exchangeRate = 1.0m;
        public decimal ExchangeRate { get => _exchangeRate; set => SetProperty(ref _exchangeRate, value); }
        
        public ObservableCollection<Bidder> Bidders { get; set; } = new();

        public string RecommendedBidderName => Bidders.FirstOrDefault(b => b.Id == RecommendedBidderId)?.Name ?? "None";
    }

    public class Bidder : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
    {
        public int Id { get; set; }
        public int BidAnalysisId { get; set; }
        
        private string _name = string.Empty;
        public string Name { get => _name; set => SetProperty(ref _name, value); }
        
        public string? Address { get; set; }
        public string? Contact { get; set; }
        
        private decimal _discount;
        public decimal Discount { get => _discount; set { if (SetProperty(ref _discount, value)) OnPropertyChanged(nameof(CalculatedTotal)); } }
        
        private decimal _miscCosts;
        public decimal MiscCosts { get => _miscCosts; set { if (SetProperty(ref _miscCosts, value)) OnPropertyChanged(nameof(CalculatedTotal)); } }
        
        public decimal TotalAmount { get; set; }
        private byte[]? _quoteScan;
        public byte[]? QuoteScan { get => _quoteScan; set => SetProperty(ref _quoteScan, value); }
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
