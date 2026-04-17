using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using JaahdLogistics.Services;

namespace JaahdLogistics.ViewModels
{
    public partial class ReportViewModel : ViewModelBase
    {
        private readonly ReportService _reportService;

        [ObservableProperty]
        private ObservableCollection<dynamic> _spendingReport = new();

        [ObservableProperty]
        private ObservableCollection<dynamic> _inventoryReport = new();

        public ReportViewModel(ReportService reportService)
        {
            _reportService = reportService;
            LoadReports();
        }

        private void LoadReports()
        {
            SpendingReport = new ObservableCollection<dynamic>(_reportService.GetSpendingPerProject());
            InventoryReport = new ObservableCollection<dynamic>(_reportService.GetInventoryStatus());
        }
    }
}
