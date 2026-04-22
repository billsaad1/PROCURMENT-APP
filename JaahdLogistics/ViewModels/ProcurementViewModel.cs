using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Windows;
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
        private bool _skipBidAnalysis;

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
            var mainVM = Application.Current.MainWindow.DataContext as MainViewModel;
            var helper = new NumberingHelper(mainVM?.ConnectionString ?? "Data Source=jaahd.db");
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
            CurrentBidAnalysis = new BidAnalysis { RFQId = CurrentRFQ.Id, Date = DateTime.Now };
            Bidders = new ObservableCollection<Bidder>();
            
            // Suggesting bidders based on historical vendors or empty
            AddBidder();
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
            CurrentBidAnalysis.Bidders = new ObservableCollection<Bidder>(Bidders.ToList());
            _dataService.SaveBidAnalysis(CurrentBidAnalysis);
        }

        [RelayCommand]
        private void CreatePO()
        {
            if (!SkipBidAnalysis && CurrentBidAnalysis.Status != "FinalApproved")
            {
                MessageBox.Show("Cannot create a Purchase Order unless the Bid Analysis is approved or 'Skip Bid Analysis' is checked.");
                return;
            }
            if (SelectedPR == null) return;

            var mainVM = Application.Current.MainWindow.DataContext as MainViewModel;
            var helper = new NumberingHelper(mainVM?.ConnectionString ?? "Data Source=jaahd.db");

            CurrentPO = new PurchaseOrder 
            { 
                PRId = SelectedPR.Id, 
                BidAnalysisId = SkipBidAnalysis ? (int?)null : CurrentBidAnalysis.Id,
                Date = DateTime.Now,
                PONumber = helper.GenerateNumber("PO", SelectedPR.ProjectId),
                Status = "Pending"
            };

            if (SkipBidAnalysis)
            {
                foreach(var item in SelectedPR.Items)
                {
                    CurrentPO.Items.Add(new POItem { Description = item.Description, Quantity = item.Quantity, Unit = item.Unit });
                }
            }
            else
            {
                var winner = Bidders.FirstOrDefault(b => b.Id == (CurrentBidAnalysis.RecommendedBidderId ?? 0));
                if (winner != null)
                {
                    CurrentPO.VendorId = winner.Id;
                    foreach(var item in winner.Items)
                    {
                        CurrentPO.Items.Add(new POItem { Description = item.Description, Quantity = item.Quantity, Unit = item.Unit, UnitPrice = item.UnitPrice });
                    }
                }
            }

            _dataService.SavePO(CurrentPO);
            MessageBox.Show("Purchase Order Created Successfully");
        }

        [RelayCommand]
        private void ApprovePO()
        {
            if (CurrentPO.Id == 0) return;
            var user = AuthService.CurrentUser;
            if (user == null) return;

            if (CurrentPO.Status == "Pending") CurrentPO.Status = "CheckedByLogistics";
            else if (CurrentPO.Status == "CheckedByLogistics") CurrentPO.Status = "ReviewedByFinance";
            else if (CurrentPO.Status == "ReviewedByFinance") CurrentPO.Status = "FinalApproved";

            _dataService.ApproveEntity("PO", CurrentPO.Id, user.Id, CurrentPO.Status);
            _dataService.SavePO(CurrentPO);
            
            MessageBox.Show($"PO {CurrentPO.PONumber} status updated to: {CurrentPO.Status}");
        }

        [RelayCommand]
        private void ApproveBidAnalysis()
        {
            if (CurrentBidAnalysis.Id == 0) return;
            var user = AuthService.CurrentUser;
            if (user == null) return;

            if (CurrentBidAnalysis.Status == "Pending") CurrentBidAnalysis.Status = "CheckedByLogistics";
            else if (CurrentBidAnalysis.Status == "CheckedByLogistics") CurrentBidAnalysis.Status = "ReviewedByFinance";
            else if (CurrentBidAnalysis.Status == "ReviewedByFinance") CurrentBidAnalysis.Status = "FinalApproved";

            _dataService.ApproveEntity("BidAnalysis", CurrentBidAnalysis.Id, user.Id, CurrentBidAnalysis.Status);
            _dataService.SaveBidAnalysis(CurrentBidAnalysis);
            
            MessageBox.Show($"Bid Analysis status updated to: {CurrentBidAnalysis.Status}");
        }

        [RelayCommand]
        private void Print(FrameworkElement element)
        {
            new PrintService().ShowPreview(element);
        }
    }
}
