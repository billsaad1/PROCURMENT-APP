using System.Collections.ObjectModel;
using System.Linq;
using JaahdLogistics.Models;
using JaahdLogistics.Services;

namespace JaahdLogistics.ViewModels
{
    public partial class DashboardViewModel : ViewModelBase
    {
        private readonly IDataService _dataService;

        public ObservableCollection<PurchaseRequisition> UserPRs { get; } = new();

        public DashboardViewModel(IDataService dataService)
        {
            _dataService = dataService;
            LoadData();
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
