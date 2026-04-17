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

        public MainViewModel(IDataService dataService, LanguageService languageService)
        {
            _dataService = dataService;
            _languageService = languageService;
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
            // Placeholder for dashboard view model
            CurrentViewModel = new ProjectViewModel(_dataService);
        }

        [RelayCommand]
        private void SwitchLanguage(string lang)
        {
            _languageService.SetLanguage(lang);
        }

        [RelayCommand]
        private void ShowView(string viewName)
        {
            switch (viewName)
            {
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
                case "Settings":
                    CurrentViewModel = new SettingsViewModel(_dataService);
                    break;
                case "Reports":
                    CurrentViewModel = new ReportViewModel(new ReportService("Data Source=jaahd.db"));
                    break;
            }
        }
    }
}
