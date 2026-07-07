using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JaahdLogistics.Models;
using JaahdLogistics.Services;

namespace JaahdLogistics.ViewModels
{
    public partial class VendorViewModel : ViewModelBase
    {
        private readonly IDataService _dataService;

        [ObservableProperty]
        private ObservableCollection<Vendor> _vendors = new();

        [ObservableProperty]
        private Vendor? _selectedVendor;

        [ObservableProperty]
        private Vendor _currentVendor = new();

        public VendorViewModel(IDataService dataService)
        {
            _dataService = dataService;
            LoadVendors();
        }

        private void LoadVendors()
        {
            Vendors = new ObservableCollection<Vendor>(_dataService.GetVendors());
        }

        [RelayCommand]
        private void NewVendor()
        {
            CurrentVendor = new Vendor();
            SelectedVendor = null;
        }

        [RelayCommand]
        private void SaveVendor()
        {
            if (string.IsNullOrWhiteSpace(CurrentVendor.Name)) return;

            _dataService.SaveVendor(CurrentVendor);
            LoadVendors();
            NewVendor();
        }

        [RelayCommand]
        private void DeleteVendor(Vendor vendor)
        {
            if (vendor == null) return;
            _dataService.DeleteVendor(vendor.Id);
            LoadVendors();
            if (CurrentVendor.Id == vendor.Id) NewVendor();
        }

        partial void OnSelectedVendorChanged(Vendor? value)
        {
            if (value != null)
            {
                // Create a clone for editing
                CurrentVendor = new Vendor
                {
                    Id = value.Id,
                    Name = value.Name,
                    Address = value.Address,
                    Contact = value.Contact,
                    Tel = value.Tel,
                    Email = value.Email,
                    Category = value.Category,
                    TaxId = value.TaxId,
                    BankInfo = value.BankInfo,
                    IsActive = value.IsActive
                };
            }
        }
    }
}
