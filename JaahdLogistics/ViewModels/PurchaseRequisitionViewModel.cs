using System;
using System.Linq;
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
        private ObservableCollection<PurchaseRequisition> _purchaseRequisitions = new();

        [ObservableProperty]
        private int _selectedTabIndex;

        partial void OnCurrentPRChanged(PurchaseRequisition value)
        {
            if (value != null)
            {
                value.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(PurchaseRequisition.Currency))
                    {
                        var rate = _dataService.GetLastExchangeRate(value.Currency);
                        if (rate > 0) value.ExchangeRate = rate;
                    }
                };
                LoadApprovals();
            }
        }

        private void LoadApprovals()
        {
            if (CurrentPR.Id == 0) return;
            var approvals = _dataService.GetApprovals("PR", CurrentPR.Id);
            foreach (var app in approvals)
            {
                string status = (string)app.Status;
                byte[]? sig = (byte[]?)app.SignatureImage;
                string? name = (string?)app.FullName;

                if (status == "CheckedByLogistics") { CurrentPR.LogisticsSignature = sig; CurrentPR.LogisticsName = name; }
                else if (status == "ReviewedByFinance") { CurrentPR.FinanceSignature = sig; CurrentPR.FinanceName = name; }
                else if (status == "ApprovedByPM") { CurrentPR.PMSignature = sig; CurrentPR.PMName = name; }
                else if (status == "FinalApproved") { CurrentPR.FinalSignature = sig; CurrentPR.FinalName = name; }
            }
            // Set requester signature from current user if new PR
            if (CurrentPR.RequesterId != 0)
            {
                var users = _dataService.GetUsers();
                var req = users.FirstOrDefault(u => u.Id == CurrentPR.RequesterId);
                if (req != null)
                {
                    CurrentPR.RequesterSignature = req.SignatureImage;
                    CurrentPR.RequesterName = req.FullName;
                }
            }
            OnPropertyChanged(nameof(CurrentPR));
        }

        [ObservableProperty]
        private string _budgetWarning = string.Empty;

        [ObservableProperty]
        private ObservableCollection<Project> _projects;

        [ObservableProperty]
        private Project? _selectedProject;

        [ObservableProperty]
        private ObservableCollection<BudgetLine> _budgetLines = new();

        [ObservableProperty]
        private Settings _settings;

        [ObservableProperty]
        private ObservableCollection<string> _previousDescriptions;

        public string[] Currencies { get; } = { "USD", "YER" };

        public PurchaseRequisitionViewModel(IDataService dataService)
        {
            _dataService = dataService;
            _projects = new ObservableCollection<Project>(_dataService.GetProjects());
            _settings = _dataService.GetSettings();
            _previousDescriptions = new ObservableCollection<string>(_dataService.GetPreviousItemDescriptions());
            LoadPRs();
        }

        private void LoadPRs()
        {
            var prs = _dataService.GetPRs();
            PurchaseRequisitions.Clear();
            foreach (var pr in prs)
            {
                PurchaseRequisitions.Add(pr);
            }
        }

        partial void OnSelectedProjectChanged(Project? value)
        {
            if (value != null)
            {
                CurrentPR.ProjectId = value.Id;
                BudgetLines = new ObservableCollection<BudgetLine>(_dataService.GetBudgetLines(value.Id));

                // Automatic Numbering (only for new PRs)
                if (string.IsNullOrEmpty(CurrentPR.PRNumber))
                {
                    var mainVM = Application.Current.MainWindow.DataContext as MainViewModel;
                    var helper = new NumberingHelper(mainVM?.ConnectionString ?? "Data Source=jaahd.db");
                    CurrentPR.PRNumber = helper.GenerateNumber("PR", value.Id);
                }
            }
        }

        [RelayCommand]
        private void AddItem()
        {
            var item = new PRItem();
            item.OnBudgetLineChanged = PopulateFromBudgetLine;
            CurrentPR.Items.Add(item);
        }

        [RelayCommand]
        private void RemoveItem(PRItem item)
        {
            if (item != null) CurrentPR.Items.Remove(item);
        }

        [RelayCommand]
        private void PopulateFromBudgetLine(PRItem item)
        {
            if (item == null || item.BudgetLineId == 0) return;
            var bl = BudgetLines.FirstOrDefault(b => b.Id == item.BudgetLineId);
            if (bl != null)
            {
                item.Description = bl.Description ?? string.Empty;
                item.Unit = bl.Unit;
                item.Quantity = bl.Quantity;
                item.UnitPrice = bl.UnitPrice;
            }
        }

        [RelayCommand]
        private void SavePR()
        {
            try
            {
                if (CurrentPR.RequesterId == 0) CurrentPR.RequesterId = AuthService.CurrentUser?.Id ?? 0;
                if (CurrentPR.Date == default) CurrentPR.Date = DateTime.Now;

                if (string.IsNullOrEmpty(CurrentPR.PRNumber))
                {
                    MessageBox.Show("PR Number is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                BudgetWarning = string.Empty;
                // Check budgets
                foreach (var item in CurrentPR.Items)
                {
                    if (item.BudgetLineId == 0)
                    {
                        MessageBox.Show($"Please select a Budget Line for item: {item.Description}", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

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

                if (!string.IsNullOrEmpty(BudgetWarning))
                {
                    MessageBox.Show("Warning: One or more items exceed the budget limit.\n\n" + BudgetWarning, "Budget Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                _dataService.SavePR(CurrentPR);
                LoadPRs();
                MessageBox.Show("PR Saved Successfully");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving PR: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void NewPR()
        {
            CurrentPR = new PurchaseRequisition();
            SelectedProject = null;
            SelectedTabIndex = 1; // Switch to Edit tab
        }

        [RelayCommand]
        private void SelectPR(PurchaseRequisition pr)
        {
            if (pr == null) return;

            // Ensure OnBudgetLineChanged is set for loaded items
            foreach (var item in pr.Items)
            {
                item.OnBudgetLineChanged = PopulateFromBudgetLine;
            }

            CurrentPR = pr;
            SelectedProject = Projects.FirstOrDefault(p => p.Id == pr.ProjectId);
            SelectedTabIndex = 1; // Switch to Edit tab
        }

        [RelayCommand]
        private void DeletePR(PurchaseRequisition pr)
        {
            if (pr == null) return;
            var result = MessageBox.Show($"Are you sure you want to delete PR {pr.PRNumber}?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _dataService.DeletePR(pr.Id);
                    LoadPRs();
                    if (CurrentPR.Id == pr.Id)
                    {
                        NewPR();
                    }
                    MessageBox.Show("PR Deleted Successfully");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Delete Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private void Approve()
        {
            try
            {
                var user = AuthService.CurrentUser;
                if (user == null)
                {
                    MessageBox.Show("You must be logged in to approve.", "Auth Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (CurrentPR.Id == 0)
                {
                    MessageBox.Show("Please save the PR before approving.", "Save Required", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Logic for multi-stage approval
                string oldStatus = CurrentPR.Status;
                if (CurrentPR.Status == "Pending") CurrentPR.Status = "CheckedByLogistics";
                else if (CurrentPR.Status == "CheckedByLogistics") CurrentPR.Status = "ReviewedByFinance";
                else if (CurrentPR.Status == "ReviewedByFinance") CurrentPR.Status = "ApprovedByPM";
                else if (CurrentPR.Status == "ApprovedByPM") CurrentPR.Status = "FinalApproved";

                if (oldStatus != CurrentPR.Status)
                {
                    _dataService.ApproveEntity("PR", CurrentPR.Id, user.Id, CurrentPR.Status);
                    _dataService.SavePR(CurrentPR);
                    LoadApprovals();
                    MessageBox.Show($"PR {CurrentPR.PRNumber} status updated to: {CurrentPR.Status}");
                }
                else
                {
                    MessageBox.Show("This PR is already fully approved or in a state that cannot be further approved.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during approval: {ex.Message}", "Approval Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void Print()
        {
            new PrintService().ShowPreview(this, "PRPrintTemplate");
        }
    }
}
