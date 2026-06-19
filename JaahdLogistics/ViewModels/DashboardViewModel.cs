using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveCharts;
using LiveCharts.Wpf;
using JaahdLogistics.Models;
using JaahdLogistics.Services;

namespace JaahdLogistics.ViewModels
{
    public partial class DashboardViewModel : ViewModelBase
    {
        private readonly IDataService _dataService;
        private readonly SyncService _syncService;

        [ObservableProperty] private string _syncStatus = "Ready";
        [ObservableProperty] private bool _isSyncing;

        [ObservableProperty] private int _totalProjects;
        [ObservableProperty] private int _pendingPRs;
        [ObservableProperty] private int _approvedPOs;
        [ObservableProperty] private decimal _totalSpending;

        public ObservableCollection<PurchaseRequisition> UserPRs { get; } = new();
        public ObservableCollection<PurchaseRequisition> ActionItems { get; } = new();
        public ObservableCollection<ProjectSpendingSummary> ProjectSpending { get; } = new();
        public ObservableCollection<PipelineSummary> PipelineDistribution { get; } = new();

        [ObservableProperty] private SeriesCollection _spendingSeries = new();
        [ObservableProperty] private SeriesCollection _monthlyTrendSeries = new();
        [ObservableProperty] private List<string> _labels = new();
        public Func<double, string> Formatter { get; set; } = value => value.ToString("N0");

        public class MonthlyTrend
        {
            public string Month { get; set; } = string.Empty;
            public int Count { get; set; }
        }

        public class PipelineSummary
        {
            public string Stage { get; set; } = string.Empty;
            public int Count { get; set; }
            public string Color { get; set; } = "#3498DB";
        }

        public class ProjectSpendingSummary
        {
            public string Name { get; set; } = string.Empty;
            public decimal Budget { get; set; }
            public decimal Spent { get; set; }
            public double PercentSpent => Budget > 0 ? (double)(Spent / Budget) * 100 : 0;
            public string ProgressColor => PercentSpent > 90 ? "Red" : (PercentSpent > 75 ? "Orange" : "Green");
        }

        public DashboardViewModel(IDataService dataService)
        {
            _dataService = dataService;
            _syncService = new SyncService(_dataService);
            Task.Run(() => RefreshDashboard()); // Run in background to avoid blocking UI
        }

        [RelayCommand]
        public void RefreshDashboard()
        {
            Application.Current.Dispatcher.Invoke(() => {
                LoadData();
                LoadAnalytics();
            });
        }

        [RelayCommand]
        private async Task SyncNow()
        {
            IsSyncing = true;
            SyncStatus = "Syncing...";
            try
            {
                await _syncService.SyncWithCloud();
                SyncStatus = $"Last sync: {System.DateTime.Now:HH:mm:ss}";
            }
            catch (System.Exception ex)
            {
                SyncStatus = "Sync failed";
                System.Windows.MessageBox.Show($"Sync error: {ex.Message}");
            }
            finally
            {
                IsSyncing = false;
            }
        }

        private void LoadData()
        {
            var user = AuthService.CurrentUser;
            if (user == null) return;

            UserPRs.Clear();
            ActionItems.Clear();

            var allPRs = _dataService.GetPRs().ToList();
            
            // 1. My PRs
            var myPRs = (user.Role == "Admin" || user.Role == "FinanceManager" || user.Role == "LogisticsManager" || user.Role == "HeadOfAssociation")
                ? allPRs 
                : allPRs.Where(p => p.RequesterId == user.Id);

            foreach (var pr in myPRs.OrderByDescending(p => p.Date))
                UserPRs.Add(pr);

            // 2. Action Required (Approvals)
            IEnumerable<PurchaseRequisition> needsApproval = new List<PurchaseRequisition>();
            if (user.Role == "LogisticsManager") needsApproval = allPRs.Where(p => p.Status == "Pending");
            else if (user.Role == "FinanceManager") needsApproval = allPRs.Where(p => p.Status == "CheckedByLogistics");
            else if (user.Role == "ProjectManager") needsApproval = allPRs.Where(p => p.Status == "ReviewedByFinance");
            else if (user.Role == "HeadOfAssociation") needsApproval = allPRs.Where(p => p.Status == "ApprovedByPM");
            else if (user.Role == "Admin") needsApproval = allPRs.Where(p => p.Status != "FinalApproved" && p.Status != "Rejected");

            foreach (var pr in needsApproval.Take(10))
                ActionItems.Add(pr);
        }

        private void LoadAnalytics()
        {
            var projects = _dataService.GetProjects().ToList();
            var allPRs = _dataService.GetPRs().ToList();
            var allPOs = _dataService.GetPOs().ToList();
            var allRFQs = _dataService.GetRFQs().ToList();
            var allBAs = _dataService.GetBidAnalyses().ToList();
            var allGRNs = _dataService.GetGRNs().ToList();

            TotalProjects = projects.Count;
            PendingPRs = allPRs.Count(p => p.Status != "FinalApproved" && p.Status != "Rejected");
            ApprovedPOs = allPOs.Count(p => p.Status == "FinalApproved" || p.Status == "ApprovedByHead");

            ProjectSpending.Clear();
            PipelineDistribution.Clear();
            TotalSpending = 0;

            // Pipeline Distribution
            PipelineDistribution.Add(new PipelineSummary { Stage = "PR", Count = allPRs.Count, Color = "#3498DB" });
            PipelineDistribution.Add(new PipelineSummary { Stage = "RFQ", Count = allRFQs.Count, Color = "#9B59B6" });
            PipelineDistribution.Add(new PipelineSummary { Stage = "BA", Count = allBAs.Count, Color = "#F1C40F" });
            PipelineDistribution.Add(new PipelineSummary { Stage = "PO", Count = allPOs.Count, Color = "#27AE60" });
            PipelineDistribution.Add(new PipelineSummary { Stage = "GRN", Count = allGRNs.Count, Color = "#E67E22" });

            // 1. Monthly Trends (Last 6 months)
            var trendValues = new ChartValues<int>();
            var labelsList = new List<string>();
            var months = Enumerable.Range(0, 6).Select(i => DateTime.Now.AddMonths(-i)).Reverse();

            foreach(var m in months)
            {
                int count = allPOs.Count(p => p.Date.Month == m.Month && p.Date.Year == m.Year);
                trendValues.Add(count);
                labelsList.Add(m.ToString("MMM"));
            }

            MonthlyTrendSeries = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "POs Issued",
                    Values = trendValues,
                    Fill = System.Windows.Media.Brushes.DodgerBlue
                }
            };
            Labels = labelsList;

            // 2. Spending Pie Chart
            SpendingSeries = new SeriesCollection();
            foreach (var proj in projects.Take(5)) // Show top 5 for visual clarity
            {
                var budgetLines = _dataService.GetBudgetLines(proj.Id);
                decimal totalBudget = budgetLines.Sum(b => b.TotalAmount);
                decimal spent = 0;

                foreach(var bl in budgetLines)
                {
                    spent += _dataService.GetSpentBudget(bl.Id);
                }

                ProjectSpending.Add(new ProjectSpendingSummary
                {
                    Name = proj.Name,
                    Budget = totalBudget,
                    Spent = spent
                });

                if (spent > 0)
                {
                    SpendingSeries.Add(new PieSeries
                    {
                        Title = proj.Code,
                        Values = new ChartValues<double> { (double)spent },
                        DataLabels = true
                    });
                }

                TotalSpending += spent;
            }
        }
    }
}
