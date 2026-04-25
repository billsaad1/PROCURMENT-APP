using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace JaahdLogistics.Models
{
    public class PurchaseRequisition : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
    {
        public int Id { get; set; }
        public string PRNumber { get; set; } = string.Empty;
        public int ProjectId { get; set; }
        public int RequesterId { get; set; }
        public DateTime Date { get; set; }
        public string? Justification { get; set; }

        private string _currency = "USD";
        public string Currency { get => _currency; set => SetProperty(ref _currency, value); }

        private decimal _exchangeRate = 1.0m;
        public decimal ExchangeRate { get => _exchangeRate; set => SetProperty(ref _exchangeRate, value); }

        private string _status = "Pending";
        public string Status { get => _status; set => SetProperty(ref _status, value); }

        public ObservableCollection<PRItem> Items { get; set; } = new();

        public decimal TotalAmount => Items.Sum(i => i.TotalPrice);

        public PurchaseRequisition()
        {
            Items.CollectionChanged += (s, e) => {
                OnPropertyChanged(nameof(TotalAmount));
                if (e.NewItems != null) {
                    foreach (PRItem item in e.NewItems) item.PropertyChanged += (s2, e2) => OnPropertyChanged(nameof(TotalAmount));
                }
            };
        }
    }

    public class PRItem : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
    {
        public int Id { get; set; }
        public int PRId { get; set; }

        private int _budgetLineId;
        public int BudgetLineId { get => _budgetLineId; set { if (SetProperty(ref _budgetLineId, value)) { OnBudgetLineChanged?.Invoke(this); } } }

        public Action<PRItem>? OnBudgetLineChanged { get; set; }

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
