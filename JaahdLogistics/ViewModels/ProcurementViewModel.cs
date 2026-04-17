using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JaahdLogistics.Models;
using JaahdLogistics.Services;

namespace JaahdLogistics.ViewModels
{
    public partial class ProcurementViewModel : ViewModelBase
    {
        private readonly IDataService _dataService;

        [ObservableProperty]
        private ObservableCollection<PurchaseRequisition> _approvedPRs = new();

        [ObservableProperty]
        private PurchaseRequisition? _selectedPR;

        [ObservableProperty]
        private RFQ _currentRFQ = new();

        [ObservableProperty]
        private BidAnalysis _currentBidAnalysis = new();

        [ObservableProperty]
        private PurchaseOrder _currentPO = new();

        public ProcurementViewModel(IDataService dataService)
        {
            _dataService = dataService;
            // Load PRs that are ready for procurement
            _approvedPRs = new ObservableCollection<PurchaseRequisition>(_dataService.GetPRs());
        }

        [RelayCommand]
        private void CreateRFQ()
        {
            if (SelectedPR == null) return;
            CurrentRFQ = new RFQ
            {
                PRId = SelectedPR.Id,
                RFQNumber = $"RFQ-{SelectedPR.PRNumber}-{DateTime.Now:yyyyMMdd}"
            };
            // In a real app, you'd save here: _dataService.SaveRFQ(CurrentRFQ);
        }

        [RelayCommand]
        private void CreateBidAnalysis()
        {
            // Logic to create bid analysis
        }

        [RelayCommand]
        private void CreatePO()
        {
            if (SelectedPR == null) return;
            CurrentPO = new PurchaseOrder
            {
                PRId = SelectedPR.Id,
                PONumber = $"PO-{SelectedPR.PRNumber}-{DateTime.Now:yyyyMMdd}"
            };
            // In a real app: _dataService.SavePO(CurrentPO);
        }
    }
}
