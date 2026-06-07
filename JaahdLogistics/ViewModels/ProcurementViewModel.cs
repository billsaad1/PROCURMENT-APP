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
    public class BidAnalysisMatrixRow : ObservableObject
    {
        public int Index { get; set; }
        public PRItem? SourceItem { get; set; }
        public ObservableCollection<BidItem> BidderPrices { get; set; } = new();
        
        public string Description => SourceItem?.Description ?? "";
        public string Unit => SourceItem?.Unit ?? "";
        public decimal Quantity => SourceItem?.Quantity ?? 0;
        public decimal EstimativeUnitPrice => SourceItem?.UnitPrice ?? 0;
        public decimal EstimativeTotal => Quantity * EstimativeUnitPrice;
    }

    public partial class ProcurementViewModel : ViewModelBase
    {
        private readonly IDataService _dataService;

        [ObservableProperty]
        private ObservableCollection<PurchaseRequisition> _approvedPRs = new();

        [ObservableProperty]
        private PurchaseRequisition? _selectedPR;

        [ObservableProperty]
        private ObservableCollection<RFQ> _rFQs = new();

        [ObservableProperty]
        private RFQ? _selectedRFQ;

        [ObservableProperty]
        private RFQ _currentRFQ = new();

        [ObservableProperty]
        private ObservableCollection<BidAnalysis> _bidAnalyses = new();

        [ObservableProperty]
        private BidAnalysis? _selectedBidAnalysis;

        [ObservableProperty]
        private BidAnalysis _currentBidAnalysis = new();

        [ObservableProperty]
        private ObservableCollection<PurchaseOrder> _pOs = new();

        [ObservableProperty]
        private PurchaseOrder? _selectedPO;

        [ObservableProperty]
        private PurchaseOrder _currentPO = new();

        [ObservableProperty]
        private bool _skipBidAnalysis;

        [ObservableProperty]
        private ObservableCollection<Bidder> _bidders = new();

        [ObservableProperty]
        private ObservableCollection<BidAnalysisMatrixRow> _matrixRows = new();

        [ObservableProperty]
        private ObservableCollection<Vendor> _vendors = new();

        [ObservableProperty]
        private Settings _settings;

        [ObservableProperty]
        private int _selectedTabIndex;

        public ProcurementViewModel(IDataService dataService)
        {
            _dataService = dataService;
            _settings = _dataService.GetSettings();
            Vendors = new ObservableCollection<Vendor>(_dataService.GetVendors());
            RefreshAll();
        }

        [RelayCommand]
        private void SetRecommendedBidder(Bidder bidder)
        {
            if (bidder == null) return;

            foreach (var b in Bidders) b.IsWinner = false;
            bidder.IsWinner = true;

            CurrentBidAnalysis.RecommendedBidderId = bidder.Id;
            CurrentBidAnalysis.RecommendationReasons = bidder.Justification;
            MessageBox.Show($"Selected {bidder.Name} as the winner.");
        }

        [RelayCommand]
        private void NewRFQ()
        {
            CurrentRFQ = new RFQ();
            SelectedPR = null;
        }

        [RelayCommand]
        private void DeleteRFQ(RFQ rfq)
        {
            if (rfq == null) return;
            var result = MessageBox.Show($"Delete RFQ {rfq.RFQNumber}?", "Confirm", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                try {
                    _dataService.DeleteRFQ(rfq.Id);
                    LoadRFQs();
                    if (CurrentRFQ.Id == rfq.Id) NewRFQ();
                } catch (Exception ex) { MessageBox.Show(ex.Message); }
            }
        }

        [RelayCommand]
        private void CreateRFQ()
        {
            if (SelectedPR == null || SelectedPR.Id == 0)
            {
                MessageBox.Show("Please select a valid Purchase Requisition first.");
                return;
            }

            try
            {
                var mainVM = Application.Current.MainWindow.DataContext as MainViewModel;
                var helper = new NumberingHelper(mainVM?.ConnectionString ?? "Data Source=jaahd.db");
                CurrentRFQ = new RFQ 
                { 
                    PRId = SelectedPR.Id, 
                    Date = DateTime.Now,
                    RFQNumber = helper.GenerateNumber("RFQ", SelectedPR.ProjectId) 
                };
                _dataService.SaveRFQ(CurrentRFQ);
                LoadRFQs();
                MessageBox.Show($"RFQ {CurrentRFQ.RFQNumber} created successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating RFQ: {ex.Message}");
            }
        }

        [RelayCommand]
        private void NewBidAnalysis()
        {
            CurrentBidAnalysis = new BidAnalysis();
            Bidders = new ObservableCollection<Bidder>();
        }

        [RelayCommand]
        private void DeleteBidAnalysis(BidAnalysis analysis)
        {
            if (analysis == null) return;
            var result = MessageBox.Show("Delete this Bid Analysis?", "Confirm", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                try {
                    _dataService.DeleteBidAnalysis(analysis.Id);
                    LoadBidAnalyses();
                    if (CurrentBidAnalysis.Id == analysis.Id) NewBidAnalysis();
                } catch (Exception ex) { MessageBox.Show(ex.Message); }
            }
        }

        [RelayCommand]
        private void CreateBidAnalysis()
        {
            if (CurrentRFQ.Id == 0) { MessageBox.Show("Please select an RFQ first."); return; }
            CurrentBidAnalysis = new BidAnalysis { 
                RFQId = CurrentRFQ.Id, 
                Date = DateTime.Now,
                Currency = SelectedPR?.Currency ?? "YER",
                ExchangeRate = SelectedPR?.ExchangeRate ?? 1.0m,
                Justification = SelectedPR?.Justification
            };
            Bidders = new ObservableCollection<Bidder>();
            MatrixRows = new ObservableCollection<BidAnalysisMatrixRow>();
            AddBidder();
        }

        [RelayCommand]
        private void AddBidder()
        {
            var bidderNumber = Bidders.Count + 1;
            var bidder = new Bidder { Name = $"Bidder {bidderNumber}", BidAnalysisId = CurrentBidAnalysis.Id };
            bidder.PropertyChanged += (s, e) => {
                if ((e.PropertyName == nameof(bidder.VendorId) || e.PropertyName == "VendorId") && bidder.VendorId.HasValue)
                {
                    var vendor = Vendors.FirstOrDefault(v => v.Id == bidder.VendorId);
                    if (vendor != null)
                    {
                        bidder.Name = vendor.Name;
                        bidder.Address = vendor.Address;
                        bidder.Tel = vendor.Tel;
                        bidder.Email = vendor.Email;
                    }
                }
            };
            
            if (SelectedPR != null)
            {
                // If it's the first bidder, initialize matrix rows
                bool isFirst = Bidders.Count == 0;
                
                foreach(var item in SelectedPR.Items)
                {
                    var bidItem = new BidItem { Description = item.Description, Quantity = item.Quantity, Unit = item.Unit };
                    bidder.Items.Add(bidItem);
                    
                    if (isFirst)
                    {
                        var row = new BidAnalysisMatrixRow { SourceItem = item };
                        row.BidderPrices.Add(bidItem);
                        MatrixRows.Add(row);
                    }
                    else
                    {
                        // Add to existing matrix rows
                        var row = MatrixRows.FirstOrDefault(r => r.Description == item.Description);
                        row?.BidderPrices.Add(bidItem);
                    }
                }
            }
            Bidders.Add(bidder);
        }

        [RelayCommand]
        private void RemoveBidder(Bidder bidder)
        {
            if (bidder == null || Bidders.Count <= 1) return;
            
            int index = Bidders.IndexOf(bidder);
            if (index < 0) return;

            foreach(var row in MatrixRows)
            {
                if (row.BidderPrices.Count > index)
                    row.BidderPrices.RemoveAt(index);
            }
            Bidders.Remove(bidder);
        }

        [RelayCommand]
        private void SaveBidAnalysis()
        {
            if (CurrentBidAnalysis == null) return;

            CurrentBidAnalysis.Bidders = new ObservableCollection<Bidder>(Bidders.ToList());
            // Sync TotalAmount for DB
            foreach (var bidder in CurrentBidAnalysis.Bidders)
            {
                bidder.TotalAmount = bidder.CalculatedTotal;
            }

            _dataService.SaveBidAnalysis(CurrentBidAnalysis);

            // If the winner was selected before save (ID was 0), re-sync RecommendedBidderId
            var winningBidder = CurrentBidAnalysis.Bidders.FirstOrDefault(b => b.IsWinner);
            if (winningBidder != null && CurrentBidAnalysis.RecommendedBidderId != winningBidder.Id)
            {
                CurrentBidAnalysis.RecommendedBidderId = winningBidder.Id;
                // Double save to persist the RecommendedBidderId after we have the real database Id for the bidder
                _dataService.SaveBidAnalysis(CurrentBidAnalysis);
            }

            LoadBidAnalyses();

            // Re-select to refresh UI and ensure IDs are synced
            if (SelectedBidAnalysis != null)
            {
                var reloaded = BidAnalyses.FirstOrDefault(b => b.Id == CurrentBidAnalysis.Id);
                if (reloaded != null) SelectedBidAnalysis = reloaded;
            }

            MessageBox.Show("Bid Analysis Saved Successfully");
        }

        [RelayCommand]
        private void UploadQuote(Bidder bidder)
        {
            if (bidder == null) return;
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.pdf"
            };
            if (dialog.ShowDialog() == true)
            {
                bidder.QuoteScan = System.IO.File.ReadAllBytes(dialog.FileName);
                MessageBox.Show("Quote Scan Uploaded.");
            }
        }

        [RelayCommand]
        private void ViewQuote(Bidder bidder)
        {
            if (bidder?.QuoteScan == null) return;
            var tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "QuoteScan.png");
            System.IO.File.WriteAllBytes(tempFile, bidder.QuoteScan);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(tempFile) { UseShellExecute = true });
        }

        [RelayCommand]
        private void NewPO()
        {
            CurrentPO = new PurchaseOrder();
        }

        [RelayCommand]
        private void DeletePO(PurchaseOrder po)
        {
            if (po == null) return;
            var result = MessageBox.Show($"Delete PO {po.PONumber}?", "Confirm", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                try {
                    _dataService.DeletePO(po.Id);
                    LoadPOs();
                    if (CurrentPO.Id == po.Id) NewPO();
                } catch (Exception ex) { MessageBox.Show(ex.Message); }
            }
        }

        [RelayCommand]
        private void CreatePO()
        {
            if (SelectedPR == null) { MessageBox.Show("Please select a Purchase Requisition first."); return; }

            if (SkipBidAnalysis && (CurrentPO.VendorId == null || CurrentPO.VendorId == 0))
            {
                MessageBox.Show("Please select a Vendor for this direct purchase.");
                return;
            }

            // Budget Check for PO
            foreach (var item in SelectedPR.Items)
            {
                if (item.BudgetLineId == 0) continue;
                
                var budgetLines = _dataService.GetBudgetLines(SelectedPR.ProjectId);
                var budgetLine = budgetLines.FirstOrDefault(b => b.Id == (int)item.BudgetLineId);
                var remaining = _dataService.GetRemainingBudget((int)item.BudgetLineId, SelectedPR.Id);
                
                decimal itemPriceInBudgetCurrency = item.TotalPrice;
                if (SelectedPR.Currency == "YER" && budgetLine?.Currency == "USD" && SelectedPR.ExchangeRate > 0)
                    itemPriceInBudgetCurrency = item.TotalPrice / SelectedPR.ExchangeRate;
                else if (SelectedPR.Currency == "USD" && budgetLine?.Currency == "YER")
                    itemPriceInBudgetCurrency = item.TotalPrice * SelectedPR.ExchangeRate;

                if (itemPriceInBudgetCurrency > remaining)
                {
                    MessageBox.Show($"Cannot create PO: Item '{item.Description}' exceeds remaining budget ({remaining:N2} {budgetLine?.Currency}).", "Budget Violation", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            var analysisToUse = SkipBidAnalysis ? null : SelectedBidAnalysis;

            if (!SkipBidAnalysis)
            {
                if (analysisToUse == null)
                {
                    MessageBox.Show("Please select an approved Bid Analysis first.");
                    return;
                }
                if (analysisToUse.Status != "FinalApproved")
                {
                    MessageBox.Show("Cannot create a Purchase Order unless the selected Bid Analysis is approved.");
                    return;
                }
            }
            if (SelectedPR == null) return;

            var mainVM = Application.Current.MainWindow.DataContext as MainViewModel;
            var helper = new NumberingHelper(mainVM?.ConnectionString ?? "Data Source=jaahd.db");

            CurrentPO = new PurchaseOrder 
            { 
                PRId = SelectedPR.Id, 
                ProjectId = SelectedPR.ProjectId,
                BidAnalysisId = SkipBidAnalysis ? (int?)null : analysisToUse?.Id,
                Date = DateTime.Now,
                PONumber = helper.GenerateNumber("PO", SelectedPR.ProjectId),
                Status = "Pending",
                Terms = Settings.POTerms,
                Currency = SelectedPR.Currency,
                ExchangeRate = SelectedPR.ExchangeRate
            };

            if (SkipBidAnalysis)
            {
                CurrentPO.Vendor = Vendors.FirstOrDefault(v => v.Id == CurrentPO.VendorId);
                foreach(var item in SelectedPR.Items)
                {
                    CurrentPO.Items.Add(new POItem { Description = item.Description, Quantity = item.Quantity, Unit = item.Unit, UnitPrice = item.UnitPrice });
                }
            }
            else if (analysisToUse != null)
            {
                var winner = analysisToUse.Bidders.FirstOrDefault(b => b.Id == (analysisToUse.RecommendedBidderId ?? 0));
                if (winner != null)
                {
                    CurrentPO.BidderId = winner.Id;
                    CurrentPO.VendorId = winner.VendorId;
                    CurrentPO.Vendor = Vendors.FirstOrDefault(v => v.Id == winner.VendorId);
                    foreach(var item in winner.Items)
                    {
                        CurrentPO.Items.Add(new POItem { Description = item.Description, Quantity = item.Quantity, Unit = item.Unit, UnitPrice = item.UnitPrice });
                    }
                }
                else
                {
                    MessageBox.Show("The selected Bid Analysis does not have a Recommended Bidder set.");
                    return;
                }
            }

            _dataService.SavePO(CurrentPO);
            MessageBox.Show("Purchase Order Created Successfully");
            LoadPOs();
            LoadApprovedPRs(); // Refresh list to remove the PR we just processed (if we implement exclusion)
        }

        [RelayCommand]
        private void ApprovePO()
        {
            if (CurrentPO.Id == 0) return;
            var user = AuthService.CurrentUser;
            if (user == null) return;

            if (CurrentPO.Status == "Pending") CurrentPO.Status = "CheckedByLogistics";
            else if (CurrentPO.Status == "CheckedByLogistics") CurrentPO.Status = "ReviewedByFinance";
            else if (CurrentPO.Status == "ReviewedByFinance") CurrentPO.Status = "ApprovedByPM";
            else if (CurrentPO.Status == "ApprovedByPM") CurrentPO.Status = "ApprovedByHead";
            else if (CurrentPO.Status == "ApprovedByHead") CurrentPO.Status = "FinalApproved";

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
            else if (CurrentBidAnalysis.Status == "ReviewedByFinance") CurrentBidAnalysis.Status = "ApprovedByPM";
            else if (CurrentBidAnalysis.Status == "ApprovedByPM") CurrentBidAnalysis.Status = "ApprovedByHead";
            else if (CurrentBidAnalysis.Status == "ApprovedByHead") CurrentBidAnalysis.Status = "FinalApproved";

            _dataService.ApproveEntity("BidAnalysis", CurrentBidAnalysis.Id, user.Id, CurrentBidAnalysis.Status);
            _dataService.SaveBidAnalysis(CurrentBidAnalysis);
            
            MessageBox.Show($"Bid Analysis status updated to: {CurrentBidAnalysis.Status}");
        }

        [RelayCommand]
        public void RefreshAll()
        {
            LoadApprovedPRs();
            LoadRFQs();
            LoadBidAnalyses();
            LoadPOs();
            Vendors = new ObservableCollection<Vendor>(_dataService.GetVendors());
        }

        [RelayCommand]
        public void LoadApprovedPRs()
        {
            var prs = _dataService.GetPRs().Where(p => p.Status == "FinalApproved").ToList();
            ApprovedPRs = new ObservableCollection<PurchaseRequisition>(prs);
        }

        [RelayCommand]
        public void LoadRFQs()
        {
            RFQs = new ObservableCollection<RFQ>(_dataService.GetRFQs());
        }

        [RelayCommand]
        public void LoadBidAnalyses()
        {
            BidAnalyses = new ObservableCollection<BidAnalysis>(_dataService.GetBidAnalyses());
        }

        [RelayCommand]
        public void LoadPOs()
        {
            POs = new ObservableCollection<PurchaseOrder>(_dataService.GetPOs());
        }

        [RelayCommand]
        private void SavePO()
        {
            if (CurrentPO == null) return;

            // Ensure vendor is loaded if VendorId exists
            if (CurrentPO.VendorId.HasValue && CurrentPO.Vendor == null)
            {
                CurrentPO.Vendor = Vendors.FirstOrDefault(v => v.Id == CurrentPO.VendorId);
            }

            _dataService.SavePO(CurrentPO);
            LoadPOs();

            // Re-select to refresh UI
            if (CurrentPO != null)
            {
                var reloaded = POs.FirstOrDefault(p => p.Id == CurrentPO.Id);
                if (reloaded != null) SelectedPO = reloaded;
            }

            MessageBox.Show("Purchase Order Saved Successfully");
        }

        partial void OnSelectedRFQChanged(RFQ? value)
        {
            if (value != null)
            {
                CurrentRFQ = value;
                var pr = _dataService.GetPRs().FirstOrDefault(p => p.Id == value.PRId);
                SelectedPR = pr;
            }
        }

        partial void OnSelectedBidAnalysisChanged(BidAnalysis? value)
        {
            if (value != null)
            {
                CurrentBidAnalysis = value;
                Bidders = value.Bidders;

                // Ensure PR is loaded for the matrix
                var rfq = _dataService.GetRFQs().FirstOrDefault(r => r.Id == value.RFQId);
                if (rfq != null)
                {
                    SelectedPR = _dataService.GetPRs().FirstOrDefault(p => p.Id == rfq.PRId);
                }
                
                // Rebuild MatrixRows
                MatrixRows = new ObservableCollection<BidAnalysisMatrixRow>();
                if (SelectedPR != null && Bidders.Count > 0)
                {
                    int i = 1;
                    foreach (var item in SelectedPR.Items)
                    {
                        var row = new BidAnalysisMatrixRow { Index = i++, SourceItem = item };
                        foreach (var bidder in Bidders)
                        {
                            var bidItem = bidder.Items.FirstOrDefault(bi => bi.Description == item.Description);
                            if (bidItem != null) row.BidderPrices.Add(bidItem);
                        }
                        MatrixRows.Add(row);
                    }
                }
            }
        }

        partial void OnSelectedPOChanged(PurchaseOrder? value)
        {
            if (value != null)
            {
                CurrentPO = value;
                if (CurrentPO.VendorId.HasValue && CurrentPO.Vendor == null)
                {
                    CurrentPO.Vendor = Vendors.FirstOrDefault(v => v.Id == CurrentPO.VendorId);
                }
            }
        }

        [RelayCommand]
        private void Print()
        {
            string template = "POPrintTemplate";
            if (SelectedTabIndex == 0) template = "RFQPrintTemplate";
            else if (SelectedTabIndex == 1) template = "BidAnalysisPrintTemplate";

            new PrintService().ShowPreview(this, template);
        }
    }
}
