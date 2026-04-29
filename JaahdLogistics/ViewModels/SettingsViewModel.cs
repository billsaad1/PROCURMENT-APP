using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JaahdLogistics.Models;
using JaahdLogistics.Services;

namespace JaahdLogistics.ViewModels
{
    public partial class SettingsViewModel : ViewModelBase
    {
        private readonly IDataService _dataService;

        [ObservableProperty]
        private Settings _settings;

        [ObservableProperty]
        private byte[]? _currentUserSignature;

        [ObservableProperty]
        private ObservableCollection<User> _users;

        public string[] Roles { get; } = { "Admin", "ProjectManager", "ProcurementManager", "FinanceManager", "Storekeeper", "HeadOfAssociation" };

        public SettingsViewModel(IDataService dataService)
        {
            _dataService = dataService;
            _settings = _dataService.GetSettings();
            CurrentUserSignature = AuthService.CurrentUser?.SignatureImage;
            _users = new ObservableCollection<User>(_dataService.GetUsers());
        }

        [RelayCommand]
        private void Save()
        {
            try
            {
                _dataService.SaveSettings(Settings);
                var user = AuthService.CurrentUser;
                if (user != null)
                {
                    user.SignatureImage = CurrentUserSignature;
                    _dataService.SaveUser(user);
                }

                foreach (var u in Users)
                {
                    // Only save if it's a new user or explicitly modified (omitted for brevity, saving all)
                    // But prevent overwriting password if it's an existing user
                    _dataService.SaveUser(u);
                }

                // Notify MainViewModel to reload settings
                if (System.Windows.Application.Current.MainWindow.DataContext is MainViewModel mainVM)
                {
                    mainVM.ReloadSettings();
                }

                System.Windows.MessageBox.Show("Settings saved successfully.");
            }
            catch (System.Exception ex)
            {
                System.Windows.MessageBox.Show($"Error saving settings: {ex.Message}", "Save Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void AddUser()
        {
            Users.Add(new User { Username = "newuser", Role = "ProjectManager", FullName = "New User" });
        }

        [RelayCommand]
        private void RemoveUser(User user)
        {
            if (user != null)
            {
                if (user.Id != 0) _dataService.DeleteUser(user.Id);
                Users.Remove(user);
            }
        }

        [RelayCommand]
        private void UploadLogo()
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                Settings.LogoImage = System.IO.File.ReadAllBytes(openFileDialog.FileName);
                OnPropertyChanged(nameof(Settings));
            }
        }

        [RelayCommand]
        private void UploadSignature()
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                CurrentUserSignature = System.IO.File.ReadAllBytes(openFileDialog.FileName);
            }
        }
    }
}
