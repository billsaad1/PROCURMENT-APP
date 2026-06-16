using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JaahdLogistics.Models;
using JaahdLogistics.Services;

namespace JaahdLogistics.ViewModels
{
    public partial class ThreeWayMatchViewModel : ViewModelBase
    {
        private readonly IDataService _dataService;

        [ObservableProperty] private string? _logisticsNameTWM;
        [ObservableProperty] private string? _financeNameTWM;
        [ObservableProperty] private string? _headNameTWM;

        [ObservableProperty] private string? _logisticsTitleTWM;
        [ObservableProperty] private string? _financeTitleTWM;
        [ObservableProperty] private string? _headTitleTWM;

        [ObservableProperty] private byte[]? _logisticsSignatureTWM;
        [ObservableProperty] private byte[]? _financeSignatureTWM;
        [ObservableProperty] private byte[]? _headSignatureTWM;

        [ObservableProperty]
        private ObservableCollection<PurchaseOrder> _completedPOs = new();

        [ObservableProperty]
        private PurchaseOrder? _selectedPO;

        [ObservableProperty]
        private ThreeWayMatch _currentMatch = new();

        [ObservableProperty]
        private ObservableCollection<ThreeWayMatch> _matches = new();

        [ObservableProperty]
        private ThreeWayMatch? _selectedMatch;

        [ObservableProperty]
        private string? _grnNumber;

        [ObservableProperty]
        private Settings _settings;

        [ObservableProperty]
        private ObservableCollection<Employee> _employees = new();

        public ThreeWayMatchViewModel(IDataService dataService)
        {
            _dataService = dataService;
            _settings = _dataService.GetSettings();
            LoadEmployees();
            RefreshAll();
        }

        private void LoadEmployees()
        {
            Employees = new ObservableCollection<Employee>(_dataService.GetEmployees());
        }

        [RelayCommand]
        public void RefreshAll()
        {
            LoadEmployees();
            LoadCompletedPOs();
            LoadMatches();
        }

        private void LoadCompletedPOs()
        {
            CompletedPOs = new ObservableCollection<PurchaseOrder>(_dataService.GetPOs().Where(p => p.Status == "FinalApproved"));
        }

        private void LoadMatches()
        {
            Matches = new ObservableCollection<ThreeWayMatch>(_dataService.GetThreeWayMatches());
        }

        partial void OnCurrentMatchChanged(ThreeWayMatch value)
        {
            if (value != null)
            {
                LoadApprovals();
            }
        }

        private void LoadApprovals()
        {
            if (CurrentMatch == null) return;

            // 1. Initial Fallback
            var logEmp = Employees.FirstOrDefault(e => e.Id == Settings.DefaultLogisticsEmployeeId);
            LogisticsNameTWM = logEmp?.NameEN ?? Settings.LogisticsManager;
            LogisticsTitleTWM = logEmp?.PositionEN ?? "Logistics Manager / مدير اللوجستيات";
            LogisticsSignatureTWM = logEmp?.SignatureImage;

            var finEmp = Employees.FirstOrDefault(e => e.Id == Settings.DefaultFinanceEmployeeId);
            FinanceNameTWM = finEmp?.NameEN ?? Settings.FinanceManager;
            FinanceTitleTWM = finEmp?.PositionEN ?? "Finance Manager / المدير المالي";
            FinanceSignatureTWM = finEmp?.SignatureImage;

            var headEmp = Employees.FirstOrDefault(e => e.Id == Settings.DefaultHeadEmployeeId);
            HeadNameTWM = headEmp?.NameEN ?? Settings.HeadOfAssociation;
            HeadTitleTWM = headEmp?.PositionEN ?? "Head / PM";
            HeadSignatureTWM = headEmp?.SignatureImage;

            if (CurrentMatch.Id == 0) return;

            // 2. Override with formal approval records
            var approvals = _dataService.GetApprovals("ThreeWayMatch", CurrentMatch.Id);
            foreach (var app in approvals)
            {
                string status = (string)app.Status;
                byte[]? sig = (byte[]?)app.SignatureImage;
                string? name = (string?)app.FullName;

                if (status == "LogisticsApproved") { LogisticsSignatureTWM = sig; LogisticsNameTWM = name; }
                else if (status == "FinanceApproved") { FinanceSignatureTWM = sig; FinanceNameTWM = name; }
                else if (status == "PMApproved") { HeadSignatureTWM = sig; HeadNameTWM = name; } // Head of Association or PM
            }
        }

        partial void OnSelectedMatchChanged(ThreeWayMatch? value)
        {
            if (value != null)
            {
                CurrentMatch = value;
                SelectedPO = CompletedPOs.FirstOrDefault(p => p.Id == value.POId);
                var grn = _dataService.GetGRNs().FirstOrDefault(g => g.Id == value.GRNId);
                GrnNumber = grn?.GRNNumber;

                // Ensure indices are present for loaded matches
                for (int i = 0; i < CurrentMatch.Items.Count; i++)
                {
                    CurrentMatch.Items[i].Index = i + 1;
                }
                LoadApprovals();
            }
        }

        [RelayCommand]
        private void NewMatch()
        {
            CurrentMatch = new ThreeWayMatch();
            SelectedPO = null;
        }

        [RelayCommand]
        private void DeleteMatch(ThreeWayMatch match)
        {
            if (match == null) return;
            var result = System.Windows.MessageBox.Show("Delete this Match record?", "Confirm", System.Windows.MessageBoxButton.YesNo);
            if (result == System.Windows.MessageBoxResult.Yes)
            {
                _dataService.DeleteThreeWayMatch(match.Id);
                LoadMatches();
                if (CurrentMatch.Id == match.Id) NewMatch();
            }
        }

        [RelayCommand]
        private void VerifyMatch()
        {
            if (SelectedPO == null) return;
            
            var grn = _dataService.GetGRNs().LastOrDefault(g => g.POId == SelectedPO.Id);
            if (grn == null)
            {
                System.Windows.MessageBox.Show("No GRN found for this PO. Match cannot be verified.");
                return;
            }

            CurrentMatch = new ThreeWayMatch 
            { 
                POId = SelectedPO.Id, 
                GRNId = grn.Id,
                Date = System.DateTime.Now,
                InvoiceNumber = grn.InvoiceNumber,
                TWMNumber = SelectedPO.PONumber + "-TWM",
                Status = "Verified" 
            };
            GrnNumber = grn.GRNNumber;

            // Populate comparison items
            CurrentMatch.Items.Clear();
            int i = 1;
            foreach (var poItem in SelectedPO.Items)
            {
                var grnItem = grn.Items.FirstOrDefault(gi => gi.POItemId == poItem.Id);
                CurrentMatch.Items.Add(new ThreeWayMatchItem
                {
                    Index = i++,
                    Description = poItem.Description,
                    Unit = poItem.Unit,
                    POPrice = poItem.UnitPrice,
                    POQuantity = poItem.Quantity,
                    GRNPrice = poItem.UnitPrice, // Assuming price remains same as PO for comparison
                    GRNQuantity = grnItem?.AcceptedQuantity ?? 0,
                    ExtractPrice = poItem.UnitPrice, // Default to PO
                    ExtractQuantity = grnItem?.AcceptedQuantity ?? 0 // Default to GRN
                });
            }
            
            _dataService.SaveThreeWayMatch(CurrentMatch);
            System.Windows.MessageBox.Show("Three-Way Match Verified Successfully");
            LoadMatches();
        }

        [RelayCommand]
        private void UploadInvoiceScan()
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                CurrentMatch.InvoiceScan = System.IO.File.ReadAllBytes(openFileDialog.FileName);
                OnPropertyChanged(nameof(CurrentMatch));
            }
        }

        [RelayCommand]
        private void Save()
        {
            if (CurrentMatch.POId == 0) return;
            try
            {
                _dataService.SaveThreeWayMatch(CurrentMatch);
                System.Windows.MessageBox.Show("Saved Successfully");
                LoadMatches();
            }
            catch (System.Exception ex) { System.Windows.MessageBox.Show(ex.Message); }
        }

        [RelayCommand]
        private void Print()
        {
            new PrintService().ShowPreview(this, "ThreeWayMatchPrintTemplate");
        }
    }
}
