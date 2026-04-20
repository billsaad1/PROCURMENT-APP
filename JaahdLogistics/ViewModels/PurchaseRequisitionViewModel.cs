using System.Windows;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JaahdLogistics.Models;
using JaahdLogistics.Services;
using JaahdLogistics.Helpers;

namespace JaahdLogistics.ViewModels
{
    public partial class PurchaseRequisitionViewModel : ViewModelBase
    {
        private readonly IDataService _dataService;

        [ObservableProperty]
        private PurchaseRequisition _currentPR = new();

        [ObservableProperty]
        private string _budgetWarning = string.Empty;

        [ObservableProperty]
        private ObservableCollection<Project> _projects;

        [ObservableProperty]
        private Project? _selectedProject;

        [ObservableProperty]
        private ObservableCollection<BudgetLine> _budgetLines = new();

        public string[] Currencies { get; } = { "USD", "YER" };

        public PurchaseRequisitionViewModel(IDataService dataService)
        {
            _dataService = dataService;
            _projects = new ObservableCollection<Project>(_dataService.GetProjects());
        }

        partial void OnSelectedProjectChanged(Project? value)
        {
            if (value != null)
            {
                CurrentPR.ProjectId = value.Id;
                BudgetLines = new ObservableCollection<BudgetLine>(_dataService.GetBudgetLines(value.Id));

                // Automatic Numbering
                var helper = new NumberingHelper("Data Source=jaahd.db");
                CurrentPR.PRNumber = helper.GenerateNumber("PR", value.Id);
            }
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

        [RelayCommand]
        private void Approve()
        {
            CurrentPR.Status = "FinalApproved";
            _dataService.SavePR(CurrentPR);
        }

        [RelayCommand]
        private void Print(FrameworkElement element)
        {
            new PrintService().Print(element);
        }
    }
}
