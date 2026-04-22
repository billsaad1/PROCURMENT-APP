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
                var mainVM = Application.Current.MainWindow.DataContext as MainViewModel;
                var helper = new NumberingHelper(mainVM?.ConnectionString ?? "Data Source=jaahd.db");
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
            if (CurrentPR.RequesterId == 0) CurrentPR.RequesterId = AuthService.CurrentUser?.Id ?? 0;
            if (CurrentPR.Date == default) CurrentPR.Date = DateTime.Now;

            BudgetWarning = string.Empty;
            // Check budgets
            foreach (var item in CurrentPR.Items)
            {
                if (item.BudgetLineId == 0) continue;

                var remaining = _dataService.GetRemainingBudget(item.BudgetLineId);
                var budgetLine = BudgetLines.FirstOrDefault(b => b.Id == item.BudgetLineId);

                decimal itemPriceInBudgetCurrency = item.TotalPrice;

                // If PR is YER and Budget is USD
                if (CurrentPR.Currency == "YER" && budgetLine?.Currency == "USD" && CurrentPR.ExchangeRate > 0)
                {
                    itemPriceInBudgetCurrency = item.TotalPrice / CurrentPR.ExchangeRate;
                }
                // If PR is USD and Budget is YER
                else if (CurrentPR.Currency == "USD" && budgetLine?.Currency == "YER")
                {
                    itemPriceInBudgetCurrency = item.TotalPrice * CurrentPR.ExchangeRate;
                }

                if (itemPriceInBudgetCurrency > remaining)
                {
                    BudgetWarning += $"Warning: Item {item.Description} exceeds remaining budget ({remaining:N2} {budgetLine?.Currency})! \n";
                }
            }

            _dataService.SavePR(CurrentPR);
            MessageBox.Show("PR Saved Successfully");
        }

        [RelayCommand]
        private void Approve()
        {
            var user = AuthService.CurrentUser;
            if (user == null) return;

            // Logic for multi-stage approval
            if (CurrentPR.Status == "Pending") CurrentPR.Status = "CheckedByLogistics";
            else if (CurrentPR.Status == "CheckedByLogistics") CurrentPR.Status = "ReviewedByFinance";
            else if (CurrentPR.Status == "ReviewedByFinance") CurrentPR.Status = "FinalApproved";

            _dataService.ApproveEntity("PR", CurrentPR.Id, user.Id, CurrentPR.Status);
            _dataService.SavePR(CurrentPR);

            MessageBox.Show($"PR {CurrentPR.PRNumber} status updated to: {CurrentPR.Status}");
        }

        [RelayCommand]
        private void Print(FrameworkElement element)
        {
            new PrintService().ShowPreview(element);
        }
    }
}
