using System;
using System.Linq;
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
            // Filter for approved POs
            _pendingPOs = new ObservableCollection<PurchaseOrder>(_dataService.GetPOs().Where(po => po.Status == "FinalApproved"));
        }

        [RelayCommand]
        private void CreateGRN()
        {
            if (SelectedPO == null) return;
            CurrentGRN = new GoodsReceivingNotes { POId = SelectedPO.Id, GRNNumber = "GRN-" + SelectedPO.PONumber };
            GrnItems.Clear();
            foreach(var item in SelectedPO.Items)
            {
                GrnItems.Add(new GRNItems
                {
                    POItemId = item.Id,
                    Description = item.Description,
                    OrderedQuantity = item.Quantity
                });
            }
        }

        [RelayCommand]
        private void SaveGRN()
        {
            _dataService.SaveGRN(CurrentGRN, GrnItems.ToList());
        }
    }
}
