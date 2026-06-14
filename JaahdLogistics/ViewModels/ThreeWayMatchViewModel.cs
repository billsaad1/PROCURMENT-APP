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

        public string? LogisticsNameTWM => Employees.FirstOrDefault(e => e.Id == Settings.DefaultLogisticsEmployeeId)?.NameEN ?? Settings.LogisticsManager;
        public string? FinanceNameTWM => Employees.FirstOrDefault(e => e.Id == Settings.DefaultFinanceEmployeeId)?.NameEN ?? Settings.FinanceManager;
        public string? HeadNameTWM => Employees.FirstOrDefault(e => e.Id == Settings.DefaultHeadEmployeeId)?.NameEN ?? Settings.HeadOfAssociation;

        public byte[]? LogisticsSignatureTWM => Employees.FirstOrDefault(e => e.Id == Settings.DefaultLogisticsEmployeeId)?.SignatureImage;
        public byte[]? FinanceSignatureTWM => Employees.FirstOrDefault(e => e.Id == Settings.DefaultFinanceEmployeeId)?.SignatureImage;
        public byte[]? HeadSignatureTWM => Employees.FirstOrDefault(e => e.Id == Settings.DefaultHeadEmployeeId)?.SignatureImage;

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
