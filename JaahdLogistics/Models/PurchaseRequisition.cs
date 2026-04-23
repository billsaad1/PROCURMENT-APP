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

    public class PRItem : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
    {
        public int Id { get; set; }
        public int PRId { get; set; }

        private int _budgetLineId;
        public int BudgetLineId { get => _budgetLineId; set => SetProperty(ref _budgetLineId, value); }

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
