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

        public SettingsViewModel(IDataService dataService)
        {
            _dataService = dataService;
            _settings = _dataService.GetSettings();
        }

        [RelayCommand]
        private void Save()
        {
            _dataService.SaveSettings(Settings);
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
    }
}
