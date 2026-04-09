using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JaahdLogistics.Models;
using JaahdLogistics.Services;

namespace JaahdLogistics.ViewModels
{
    public partial class PurchaseRequisitionViewModel : ViewModelBase
    {
        private readonly IDataService _dataService;

        [ObservableProperty]
        private PurchaseRequisition _currentPR = new();

        [ObservableProperty]
        private string _budgetWarning = string.Empty;

        public PurchaseRequisitionViewModel(IDataService dataService)
        {
            _dataService = dataService;
        }

        [RelayCommand]
        private void AddItem()
        {
            CurrentPR.Items.Add(new PRItem());
        }

        [RelayCommand]
        private void SavePR()
        {
            // Check budgets
            foreach (var item in CurrentPR.Items)
            {
                var remaining = _dataService.GetRemainingBudget(item.BudgetLineId);
                if (item.TotalPrice > remaining)
                {
                    BudgetWarning = $"Warning: Item {item.Description} exceeds remaining budget!";
                }
            }

            _dataService.SavePR(CurrentPR);
        }
    }
}
