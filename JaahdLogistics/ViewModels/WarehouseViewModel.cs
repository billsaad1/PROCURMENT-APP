using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JaahdLogistics.Models;
using JaahdLogistics.Services;

namespace JaahdLogistics.ViewModels
{
    public partial class WarehouseViewModel : ViewModelBase
    {
        private readonly IDataService _dataService;

        [ObservableProperty]
        private ObservableCollection<PurchaseOrder> _pendingPOs = new();

        [ObservableProperty]
        private PurchaseOrder? _selectedPO;

        [ObservableProperty]
        private GoodsReceivingNotes _currentGRN = new();

        [ObservableProperty]
        private ObservableCollection<GRNItems> _grnItems = new();

        public WarehouseViewModel(IDataService dataService)
        {
            _dataService = dataService;
            // In a real app, filter for approved POs
            _pendingPOs = new ObservableCollection<PurchaseOrder>(_dataService.GetPRs().Select(pr => new PurchaseOrder { PONumber = "PO-" + pr.PRNumber, Id = pr.Id }));
        }

        [RelayCommand]
        private void CreateGRN()
        {
            if (SelectedPO == null) return;
            CurrentGRN = new GoodsReceivingNotes { POId = SelectedPO.Id, GRNNumber = "GRN-" + SelectedPO.PONumber };
            // Populate items from PO...
        }

        [RelayCommand]
        private void SaveGRN()
        {
            _dataService.SaveGRN(CurrentGRN, GrnItems.ToList());
        }
    }
}
