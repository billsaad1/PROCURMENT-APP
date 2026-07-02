using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JaahdLogistics.Models;
using JaahdLogistics.Services;

namespace JaahdLogistics.ViewModels
{
    public partial class ReportViewModel : ViewModelBase
    {
        private readonly ReportService _reportService;
        private readonly IDataService _dataService;

        private string T(string key) => System.Windows.Application.Current.TryFindResource(key)?.ToString() ?? key;

        [ObservableProperty] private ObservableCollection<dynamic> _spendingReport = new();
        [ObservableProperty] private ObservableCollection<dynamic> _inventoryReport = new();
        [ObservableProperty] private ObservableCollection<dynamic> _vendorReport = new();
        [ObservableProperty] private ObservableCollection<dynamic> _pipelineReport = new();

        [ObservableProperty] private Settings _settings;
        [ObservableProperty] private string _reportTitle = "Report";
        [ObservableProperty] private object? _reportContent;
        [ObservableProperty] private int _selectedTabIndex;

        public ReportViewModel(ReportService reportService, IDataService dataService)
        {
            _reportService = reportService;
            _dataService = dataService;
            _settings = _dataService.GetSettings();
            LoadReports();
        }

        [RelayCommand]
        public void LoadReports()
        {
            SpendingReport = new ObservableCollection<dynamic>(_reportService.GetSpendingPerProject());
            InventoryReport = new ObservableCollection<dynamic>(_reportService.GetInventoryStatus());
            VendorReport = new ObservableCollection<dynamic>(_reportService.GetVendorHistory());
            PipelineReport = new ObservableCollection<dynamic>(_reportService.GetProcurementPipeline());
        }

        [RelayCommand]
        private void PrintReport(System.Windows.Controls.DataGrid grid)
        {
            if (grid == null) return;

            ReportTitle = SelectedTabIndex switch
            {
                0 => T("ProjectSpendingReport"),
                1 => T("InventoryStatusReport"),
                2 => T("VendorHistoryReport"),
                3 => T("ProcurementVolume"),
                _ => T("Reports")
            };

            // Clone the grid or create a simplified view for printing
            var printGrid = new System.Windows.Controls.DataGrid
            {
                ItemsSource = grid.ItemsSource,
                AutoGenerateColumns = false,
                BorderThickness = new System.Windows.Thickness(1),
                BorderBrush = System.Windows.Media.Brushes.Black,
                GridLinesVisibility = System.Windows.Controls.DataGridGridLinesVisibility.All,
                HeadersVisibility = System.Windows.Controls.DataGridHeadersVisibility.Column,
                IsReadOnly = true,
                CanUserAddRows = false,
                FontSize = 10,
                ColumnHeaderStyle = grid.Resources[typeof(System.Windows.Controls.Primitives.DataGridColumnHeader)] as System.Windows.Style
            };

            foreach (var col in grid.Columns)
            {
                if (col is System.Windows.Controls.DataGridTextColumn textCol)
                {
                    var header = textCol.Header;
                    if (header is string s && s == "Stage")
                    {
                        // Pipeline stage names need translation or they stay as PR, RFQ...
                    }

                    printGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn
                    {
                        Header = header,
                        Binding = textCol.Binding,
                        Width = textCol.Width
                    });
                }
            }

            ReportContent = printGrid;
            new PrintService().ShowPreview(this, "ReportPrintTemplate");
        }

        [RelayCommand]
        private void DirectPrintReport(System.Windows.Controls.DataGrid grid)
        {
            if (grid == null) return;
            ReportTitle = SelectedTabIndex switch
            {
                0 => T("ProjectSpendingReport"),
                1 => T("InventoryStatusReport"),
                2 => T("VendorHistoryReport"),
                3 => T("ProcurementVolume"),
                _ => T("Reports")
            };
            ReportContent = grid; // Use grid directly for direct print
            new PrintService().DirectPrint(this, "ReportPrintTemplate");
        }
    }
}
