using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JaahdLogistics.Models;
using JaahdLogistics.Services;

namespace JaahdLogistics.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        private readonly IDataService _dataService;
        private readonly LanguageService _languageService;

        [ObservableProperty]
        private ViewModelBase? _currentViewModel;

        [ObservableProperty]
        private User? _currentUser;

        [ObservableProperty]
        private FlowDirection _currentFlowDirection = FlowDirection.LeftToRight;

        [ObservableProperty]
        private Settings _settings = new();

        public void ReloadSettings()
        {
            Settings = _dataService.GetSettings();
        }

        public string ConnectionString { get; }

        public MainViewModel(IDataService dataService, LanguageService languageService, string connectionString)
        {
            _dataService = dataService;
            _languageService = languageService;
            ConnectionString = connectionString;
            Settings = _dataService.GetSettings();
            ShowLogin();
        }

        private void ShowLogin()
        {
            var loginVM = new LoginViewModel(_dataService);
            loginVM.OnLoginSuccess += (user) => {
                CurrentUser = user;
                AuthService.Login(user);
                ShowDashboard();
            };
            CurrentViewModel = loginVM;
        }

        private void ShowDashboard()
        {
            CurrentViewModel = new DashboardViewModel(_dataService);
        }

        [RelayCommand]
        private void SwitchLanguage(string lang)
        {
            _languageService.SetLanguage(lang);
            CurrentFlowDirection = lang == "ar" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

            // Trigger refresh of current view if it supports it
            if (CurrentViewModel != null)
            {
                // Force a reload of signatory info for the active view
                if (CurrentViewModel is PurchaseRequisitionViewModel prVM)
                {
                    // Private methods in other ViewModels can't be called directly easily without exposure
                    // but we can trigger a property change or a public Refresh
                    var current = prVM.CurrentPR;
                    prVM.CurrentPR = null!;
                    prVM.CurrentPR = current;
                }
                else if (CurrentViewModel is ProcurementViewModel pVM)
                {
                    var ba = pVM.CurrentBidAnalysis;
                    pVM.CurrentBidAnalysis = null!;
                    pVM.CurrentBidAnalysis = ba;

                    var po = pVM.CurrentPO;
                    pVM.CurrentPO = null!;
                    pVM.CurrentPO = po;
                }
                else if (CurrentViewModel is ThreeWayMatchViewModel twmVM)
                {
                    var m = twmVM.CurrentMatch;
                    twmVM.CurrentMatch = null!;
                    twmVM.CurrentMatch = m;
                }
            }
        }

        [RelayCommand]
        private void ShowView(string viewName)
        {
            switch (viewName)
            {
                case "Dashboard":
                    ShowDashboard();
                    break;
                case "Projects":
                    CurrentViewModel = new ProjectViewModel(_dataService);
                    break;
                case "PR":
                    CurrentViewModel = new PurchaseRequisitionViewModel(_dataService);
                    break;
                case "Procurement":
                    CurrentViewModel = new ProcurementViewModel(_dataService);
                    break;
                case "Warehouse":
                    CurrentViewModel = new WarehouseViewModel(_dataService);
                    break;
                case "Match":
                    CurrentViewModel = new ThreeWayMatchViewModel(_dataService);
                    break;
                case "Vendors":
                    CurrentViewModel = new VendorViewModel(_dataService);
                    break;
                case "Employees":
                    CurrentViewModel = new EmployeeViewModel(_dataService);
                    break;
                case "Settings":
                    CurrentViewModel = new SettingsViewModel(_dataService);
                    break;
                case "Reports":
                    CurrentViewModel = new ReportViewModel(new ReportService(ConnectionString), _dataService);
                    break;
            }
        }
    }
}
