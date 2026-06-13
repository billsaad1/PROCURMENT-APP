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

        [ObservableProperty]
        private ObservableCollection<GoodsReceivingNotes> _grns = new();

        [ObservableProperty]
        private GoodsReceivingNotes? _selectedGRN;

        [ObservableProperty]
        private Settings _settings;

        public WarehouseViewModel(IDataService dataService)
        {
            _dataService = dataService;
            _settings = _dataService.GetSettings();
            RefreshAll();
        }

        [RelayCommand]
        public void RefreshAll()
        {
            LoadPendingPOs();
            LoadGRNs();
        }

        private void LoadPendingPOs()
        {
            PendingPOs = new ObservableCollection<PurchaseOrder>(_dataService.GetPOs().Where(po => po.Status == "FinalApproved"));
        }

        private void LoadGRNs()
        {
            Grns = new ObservableCollection<GoodsReceivingNotes>(_dataService.GetGRNs());
        }

        partial void OnSelectedGRNChanged(GoodsReceivingNotes? value)
        {
            if (value != null)
            {
                CurrentGRN = value;
                GrnItems = new ObservableCollection<GRNItems>(value.Items);
            }
        }

        [RelayCommand]
        private void NewGRN()
        {
            CurrentGRN = new GoodsReceivingNotes();
            GrnItems.Clear();
            SelectedPO = null;
        }

        [RelayCommand]
        private void DeleteGRN(GoodsReceivingNotes grn)
        {
            if (grn == null) return;
            var result = System.Windows.MessageBox.Show($"Delete GRN {grn.GRNNumber}?", "Confirm", System.Windows.MessageBoxButton.YesNo);
            if (result == System.Windows.MessageBoxResult.Yes)
            {
                try {
                    _dataService.DeleteGRN(grn.Id);
                    LoadGRNs();
                    if (CurrentGRN.Id == grn.Id) NewGRN();
                } catch (Exception ex) { System.Windows.MessageBox.Show(ex.Message); }
            }
        }

        [RelayCommand]
        private void CreateGRN()
        {
            if (SelectedPO == null) return;
            CurrentGRN = new GoodsReceivingNotes 
            { 
                POId = SelectedPO.Id, 
                GRNNumber = "GRN-" + SelectedPO.PONumber,
                ReceiverId = AuthService.CurrentUser?.Id ?? 0,
                Date = DateTime.Now
            };
            GrnItems.Clear();
            foreach(var item in SelectedPO.Items)
            {
                GrnItems.Add(new GRNItems 
                { 
                    POItemId = item.Id, 
                    Description = item.Description, 
                    Unit = item.Unit,
                    OrderedQuantity = item.Quantity 
                });
            }
        }

        [RelayCommand]
        private void SaveGRN()
        {
            try {
                _dataService.SaveGRN(CurrentGRN, GrnItems.ToList());
                System.Windows.MessageBox.Show("GRN Saved Successfully");
                LoadGRNs();
            } catch (Exception ex) { System.Windows.MessageBox.Show(ex.Message); }
        }

        [RelayCommand]
        private void Print()
        {
            new PrintService().ShowPreview(this, "GRNPrintTemplate");
        }
    }
}
