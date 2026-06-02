using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JaahdLogistics.Models;
using JaahdLogistics.Services;

namespace JaahdLogistics.ViewModels
{
    public partial class ConflictResolverViewModel : ViewModelBase
    {
        private readonly IDataService _dataService;

        [ObservableProperty]
        private ObservableCollection<ConflictItem> _conflicts = new();

        public ConflictResolverViewModel(IDataService dataService)
        {
            _dataService = dataService;
            // In a real app, this would be populated by the SyncService
        }

        [RelayCommand]
        private void Resolve(ConflictItem item)
        {
            // Logic to choose one version
            Conflicts.Remove(item);
        }
    }

    public class ConflictItem
    {
        public string EntityName { get; set; } = string.Empty;
        public string LocalVersion { get; set; } = string.Empty;
        public string RemoteVersion { get; set; } = string.Empty;
    }
}
