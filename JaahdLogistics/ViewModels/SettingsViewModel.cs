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

        public SettingsViewModel(IDataService dataService)
        {
            _dataService = dataService;
            _settings = _dataService.GetSettings();
            CurrentUserSignature = AuthService.CurrentUser?.SignatureImage;
        }

        [RelayCommand]
        private void Save()
        {
            _dataService.SaveSettings(Settings);
            var user = AuthService.CurrentUser;
            if (user != null)
            {
                user.SignatureImage = CurrentUserSignature;
                _dataService.SaveUser(user);
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
