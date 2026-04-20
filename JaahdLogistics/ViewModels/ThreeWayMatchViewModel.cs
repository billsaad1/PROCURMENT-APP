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

        public ThreeWayMatchViewModel(IDataService dataService)
        {
            _dataService = dataService;
            _completedPOs = new ObservableCollection<PurchaseOrder>(_dataService.GetPOs().Where(p => p.Status == "FinalApproved"));
        }

        [RelayCommand]
        private void VerifyMatch()
        {
            if (SelectedPO == null) return;
            // Logic to compare PO, GRN, and Invoice
            CurrentMatch = new ThreeWayMatch { POId = SelectedPO.Id, Status = "Verified" };
        }
    }
}
