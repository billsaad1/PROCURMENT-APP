using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JaahdLogistics.Models;
using JaahdLogistics.Services;

namespace JaahdLogistics.ViewModels
{
    public partial class DashboardViewModel : ViewModelBase
    {
        private readonly IDataService _dataService;
        private readonly SyncService _syncService;

        [ObservableProperty]
        private string _syncStatus = "Ready";

        [ObservableProperty]
        private bool _isSyncing;

        public ObservableCollection<PurchaseRequisition> UserPRs { get; } = new();

        public DashboardViewModel(IDataService dataService)
        {
            _dataService = dataService;
            _syncService = new SyncService(_dataService);
            LoadData();
        }

        [RelayCommand]
        private async Task SyncNow()
        {
            IsSyncing = true;
            SyncStatus = "Syncing...";
            try
            {
                await _syncService.SyncWithCloud();
                SyncStatus = $"Last sync: {System.DateTime.Now:HH:mm:ss}";
            }
            catch (System.Exception ex)
            {
                SyncStatus = "Sync failed";
                System.Windows.MessageBox.Show($"Sync error: {ex.Message}");
            }
            finally
            {
                IsSyncing = false;
            }
        }

        private void LoadData()
        {
            var user = AuthService.CurrentUser;
            if (user == null) return;

            var allPRs = _dataService.GetPRs();

            // If user is PM/Officer, only show their PRs. Admins see all.
            var filtered = (user.Role == "Admin" || user.Role == "FinanceManager")
                ? allPRs
                : allPRs.Where(p => p.RequesterId == user.Id);

            foreach (var pr in filtered)
            {
                UserPRs.Add(pr);
            }
        }
    }
}
