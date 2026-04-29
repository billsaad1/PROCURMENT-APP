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

        public ThreeWayMatchViewModel(IDataService dataService)
        {
            _dataService = dataService;
            RefreshAll();
        }

        [RelayCommand]
        public void RefreshAll()
        {
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

            var grn = _dataService.GetGRNs().FirstOrDefault(g => g.POId == SelectedPO.Id);
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
                Status = "Verified"
            };

            _dataService.SaveThreeWayMatch(CurrentMatch);
            System.Windows.MessageBox.Show("Three-Way Match Verified Successfully");
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
            _dataService.SaveThreeWayMatch(CurrentMatch);
            System.Windows.MessageBox.Show("Saved Successfully");
            LoadMatches();
        }
    }
}
