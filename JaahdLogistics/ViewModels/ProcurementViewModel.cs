using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JaahdLogistics.Models;
using JaahdLogistics.Services;
using JaahdLogistics.Helpers;

namespace JaahdLogistics.ViewModels
{
    public partial class ProcurementViewModel : ViewModelBase
    {
        private readonly IDataService _dataService;

        [ObservableProperty]
        private ObservableCollection<PurchaseRequisition> _approvedPRs = new();

        [ObservableProperty]
        private PurchaseRequisition? _selectedPR;

        [ObservableProperty]
        private RFQ _currentRFQ = new();

        [ObservableProperty]
        private BidAnalysis _currentBidAnalysis = new();

        [ObservableProperty]
        private PurchaseOrder _currentPO = new();

        [ObservableProperty]
        private ObservableCollection<Bidder> _bidders = new();

        public ProcurementViewModel(IDataService dataService)
        {
            _dataService = dataService;
            // Load PRs that are ready for procurement (Approved)
            _approvedPRs = new ObservableCollection<PurchaseRequisition>(_dataService.GetPRs().Where(p => p.Status == "FinalApproved"));
        }

        [RelayCommand]
        private void CreateRFQ()
        {
            if (SelectedPR == null) return;
            var helper = new NumberingHelper("Data Source=jaahd.db");
            CurrentRFQ = new RFQ
            {
                PRId = SelectedPR.Id,
                RFQNumber = helper.GenerateNumber("RFQ", SelectedPR.ProjectId)
            };
            _dataService.SaveRFQ(CurrentRFQ);
        }

        [RelayCommand]
        private void CreateBidAnalysis()
        {
            if (CurrentRFQ.Id == 0) return;
            CurrentBidAnalysis = new BidAnalysis { RFQId = CurrentRFQ.Id };
            Bidders = new ObservableCollection<Bidder>();
            // Logic to populate bidders...
        }

        [RelayCommand]
        private void AddBidder()
        {
            var bidder = new Bidder { Name = "New Bidder", BidAnalysisId = CurrentBidAnalysis.Id };
            if (SelectedPR != null)
            {
                foreach(var item in SelectedPR.Items)
                {
                    bidder.Items.Add(new BidItem { Description = item.Description, Quantity = item.Quantity, Unit = item.Unit });
                }
            }
            Bidders.Add(bidder);
        }

        [RelayCommand]
        private void SaveBidAnalysis()
        {
            CurrentBidAnalysis.Bidders = Bidders.ToList();
            _dataService.SaveBidAnalysis(CurrentBidAnalysis);
        }

        [RelayCommand]
        private void CreatePO()
        {
            if (SelectedPR == null || CurrentBidAnalysis.RecommendedBidderId == null) return;
            var winner = Bidders.FirstOrDefault(b => b.Id == CurrentBidAnalysis.RecommendedBidderId);
            if (winner == null) return;

            var helper = new NumberingHelper("Data Source=jaahd.db");
            CurrentPO = new PurchaseOrder
            {
                PRId = SelectedPR.Id,
                BidAnalysisId = CurrentBidAnalysis.Id,
                VendorId = winner.Id,
                PONumber = helper.GenerateNumber("PO", SelectedPR.ProjectId)
            };

            foreach(var item in winner.Items)
            {
                CurrentPO.Items.Add(new POItem { Description = item.Description, Quantity = item.Quantity, Unit = item.Unit, UnitPrice = item.UnitPrice });
            }

            _dataService.SavePO(CurrentPO);
        }
    }
}
