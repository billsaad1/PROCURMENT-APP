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
            _ = RefreshDashboard();
        }

        [RelayCommand]
        public async Task RefreshDashboard()
        {
            try
            {
                await Task.Run(() => {
                    var user = AuthService.CurrentUser;
                    if (user == null) return;

                    // 1. Data Retrieval (Background)
                    var allProjects = _dataService.GetProjects().ToList();
                    var allPRs = _dataService.GetPRs().ToList();
                    var allPOs = _dataService.GetPOs().ToList();
                    var allRFQs = _dataService.GetRFQs().ToList();
                    var allBAs = _dataService.GetBidAnalyses().ToList();
                    var allGRNs = _dataService.GetGRNs().ToList();

                    var myPRs = (user.Role == "Admin" || user.Role == "FinanceManager" || user.Role == "LogisticsManager" || user.Role == "HeadOfAssociation")
                        ? allPRs
                        : allPRs.Where(p => p.RequesterId == user.Id).ToList();

                    IEnumerable<PurchaseRequisition> needsApproval = new List<PurchaseRequisition>();
                    if (user.Role == "LogisticsManager") needsApproval = allPRs.Where(p => p.Status == "Pending");
                    else if (user.Role == "FinanceManager") needsApproval = allPRs.Where(p => p.Status == "CheckedByLogistics");
                    else if (user.Role == "ProjectManager") needsApproval = allPRs.Where(p => p.Status == "ReviewedByFinance");
                    else if (user.Role == "HeadOfAssociation") needsApproval = allPRs.Where(p => p.Status == "ApprovedByPM");
                    else if (user.Role == "Admin") needsApproval = allPRs.Where(p => p.Status != "FinalApproved" && p.Status != "Rejected");

                    var actionItems = needsApproval.Take(10).ToList();

                    // Analytics data prep
                    var projectSummaries = new List<ProjectSpendingSummary>();
                    decimal totalSpendingAcrossAll = 0;
                    var pieSeries = new SeriesCollection();

                    foreach (var proj in allProjects.OrderByDescending(p => _dataService.GetBudgetLines(p.Id).Sum(bl => _dataService.GetSpentBudget(bl.Id))).Take(5))
                    {
                        var budgetLines = _dataService.GetBudgetLines(proj.Id);
                        decimal totalBudget = budgetLines.Sum(b => b.TotalAmount);
                        decimal spent = budgetLines.Sum(bl => _dataService.GetSpentBudget(bl.Id));

                        projectSummaries.Add(new ProjectSpendingSummary { Name = proj.Name, Budget = totalBudget, Spent = spent });
                        totalSpendingAcrossAll += spent;

                        if (spent > 0)
                        {
                            Application.Current.Dispatcher.Invoke(() => {
                                pieSeries.Add(new PieSeries
                                {
                                    Title = proj.Code,
                                    Values = new ChartValues<double> { (double)spent },
                                    DataLabels = true,
                                    LabelPoint = chartPoint => $"{proj.Code}: {chartPoint.Y:N0} ({chartPoint.Participation:P1})"
                                });
                            });
                        }
                    }

                    // Trend Data
                    var trendValues = new ChartValues<int>();
                    var labelsList = new List<string>();
                    var months = Enumerable.Range(0, 6).Select(i => DateTime.Now.AddMonths(-i)).Reverse();
                    foreach(var m in months)
                    {
                        trendValues.Add(allPOs.Count(p => p.Date.Month == m.Month && p.Date.Year == m.Year));
                        labelsList.Add(m.ToString("MMM"));
                    }

                    // 2. UI Updates (Dispatcher)
                    Application.Current.Dispatcher.Invoke(() => {
                        TotalProjects = allProjects.Count;
                        PendingPRs = allPRs.Count(p => p.Status != "FinalApproved" && p.Status != "Rejected");
                        ApprovedPOs = allPOs.Count(p => p.Status == "FinalApproved" || p.Status == "ApprovedByHead");
                        TotalSpending = totalSpendingAcrossAll;

                        UserPRs.Clear();
                        foreach (var pr in myPRs.OrderByDescending(p => p.Date)) UserPRs.Add(pr);

                        ActionItems.Clear();
                        foreach (var pr in actionItems) ActionItems.Add(pr);

                        ProjectSpending.Clear();
                        foreach (var s in projectSummaries) ProjectSpending.Add(s);

                        PipelineDistribution.Clear();
                        PipelineDistribution.Add(new PipelineSummary { Stage = "PR", Count = allPRs.Count, Color = "#3498DB" });
                        PipelineDistribution.Add(new PipelineSummary { Stage = "RFQ", Count = allRFQs.Count, Color = "#9B59B6" });
                        PipelineDistribution.Add(new PipelineSummary { Stage = "BA", Count = allBAs.Count, Color = "#F1C40F" });
                        PipelineDistribution.Add(new PipelineSummary { Stage = "PO", Count = allPOs.Count, Color = "#27AE60" });
                        PipelineDistribution.Add(new PipelineSummary { Stage = "GRN", Count = allGRNs.Count, Color = "#E67E22" });

                        SpendingSeries = pieSeries;
                        Labels = labelsList;
                        MonthlyTrendSeries = new SeriesCollection
                        {
                            new LineSeries
                            {
                                Title = "Orders Issued",
                                Values = trendValues,
                                StrokeThickness = 3,
                                PointGeometrySize = 10,
                                Fill = System.Windows.Media.Brushes.Transparent
                            }
                        };
                    });
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Dashboard Refresh Error: {ex.Message}");
            }
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
                await RefreshDashboard();
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
    }
}
