using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JaahdLogistics.Services;
using JaahdLogistics.Models;

namespace JaahdLogistics.ViewModels
{
    public partial class LoginViewModel : ViewModelBase
    {
        private readonly IDataService _dataService;

        [ObservableProperty]
        private string _username = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        public event Action<User>? OnLoginSuccess;

        public LoginViewModel(IDataService dataService)
        {
            _dataService = dataService;
        }

        [RelayCommand]
        private void Login()
        {
            var user = _dataService.Authenticate(Username, Password);
            if (user != null)
            {
                OnLoginSuccess?.Invoke(user);
            }
            else
            {
                ErrorMessage = "Invalid username or password";
            }
        }
    }
}
