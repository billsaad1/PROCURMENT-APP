using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

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
        public string? Currency { get; set; }
        public decimal ExchangeRate { get; set; } = 1.0m;
        public ObservableCollection<Bidder> Bidders { get; set; } = new();

        public string RecommendedBidderName => Bidders.FirstOrDefault(b => b.Id == RecommendedBidderId)?.Name ?? "None";
    }

    public class Bidder : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
    {
        public int Id { get; set; }
        public int BidAnalysisId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Contact { get; set; }
        public decimal Discount { get; set; }
        public decimal MiscCosts { get; set; }
        public decimal TotalAmount { get; set; }
        private byte[]? _quoteScan;
        public byte[]? QuoteScan { get => _quoteScan; set => SetProperty(ref _quoteScan, value); }
        public ObservableCollection<BidItem> Items { get; set; } = new();

        public decimal CalculatedSubTotal => Items.Sum(i => i.TotalPrice);
        public decimal CalculatedTotal => CalculatedSubTotal - Discount + MiscCosts;
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
