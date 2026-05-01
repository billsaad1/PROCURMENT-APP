using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace JaahdLogistics.Models
{
    public partial class PurchaseRequisition : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
    {
        public int Id { get; set; }

        private string _prNumber = string.Empty;
        public string PRNumber { get => _prNumber; set => SetProperty(ref _prNumber, value); }

        private int _projectId;
        public int ProjectId { get => _projectId; set => SetProperty(ref _projectId, value); }

        private int _requesterId;
        public int RequesterId { get => _requesterId; set => SetProperty(ref _requesterId, value); }

        private DateTime _date;
        public DateTime Date { get => _date; set => SetProperty(ref _date, value); }

        private string? _justification;
        public string? Justification { get => _justification; set => SetProperty(ref _justification, value); }

        private string _currency = "USD";
        public string Currency { get => _currency; set => SetProperty(ref _currency, value); }

        private decimal _exchangeRate = 1.0m;
        public decimal ExchangeRate { get => _exchangeRate; set => SetProperty(ref _exchangeRate, value); }

        private string _status = "Pending";
        public string Status { get => _status; set => SetProperty(ref _status, value); }

        private byte[]? _requesterSignature;
        public byte[]? RequesterSignature { get => _requesterSignature; set => SetProperty(ref _requesterSignature, value); }

        private byte[]? _logisticsSignature;
        public byte[]? LogisticsSignature { get => _logisticsSignature; set => SetProperty(ref _logisticsSignature, value); }

        private byte[]? _financeSignature;
        public byte[]? FinanceSignature { get => _financeSignature; set => SetProperty(ref _financeSignature, value); }

        private byte[]? _pmSignature;
        public byte[]? PMSignature { get => _pmSignature; set => SetProperty(ref _pmSignature, value); }

        private byte[]? _finalSignature;
        public byte[]? FinalSignature { get => _finalSignature; set => SetProperty(ref _finalSignature, value); }

        private string? _requesterName;
        public string? RequesterName { get => _requesterName; set => SetProperty(ref _requesterName, value); }

        private string? _logisticsName;
        public string? LogisticsName { get => _logisticsName; set => SetProperty(ref _logisticsName, value); }

        private string? _financeName;
        public string? FinanceName { get => _financeName; set => SetProperty(ref _financeName, value); }

        private string? _pmName;
        public string? PMName { get => _pmName; set => SetProperty(ref _pmName, value); }

        private string? _finalName;
        public string? FinalName { get => _finalName; set => SetProperty(ref _finalName, value); }

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
