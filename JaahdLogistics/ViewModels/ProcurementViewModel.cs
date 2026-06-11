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

        partial void OnCurrentPOChanged(PurchaseOrder value)
        {
            if (value == null)
            {
                CurrentPO = new PurchaseOrder();
            }
            SubscribeToCurrentPO();
        }

        [ObservableProperty]
        private bool _skipBidAnalysis;

        [ObservableProperty]
        private ObservableCollection<Bidder> _bidders = new();

        [ObservableProperty]
        private ObservableCollection<BidAnalysisMatrixRow> _matrixRows = new();

        [ObservableProperty]
        private ObservableCollection<Vendor> _vendors = new();

        [ObservableProperty]
        private ObservableCollection<Project> _projects = new();

        [ObservableProperty]
        private ObservableCollection<PurchaseRequisition> _allPRs = new();

        [ObservableProperty]
        private Settings _settings;

        [ObservableProperty]
        private int _selectedTabIndex;

        public ProcurementViewModel(IDataService dataService)
        {
            _dataService = dataService;
            _settings = _dataService.GetSettings();
            Vendors = new ObservableCollection<Vendor>(_dataService.GetVendors());

            SubscribeToCurrentPO();
            RefreshAll();
        }

        private void SubscribeToCurrentPO()
        {
            if (CurrentPO == null) return;
            CurrentPO.PropertyChanged += (s, e) => {
                if (e.PropertyName == nameof(PurchaseOrder.VendorId))
                {
                    if (CurrentPO.VendorId.HasValue && CurrentPO.VendorId > 0)
                    {
                        var vendor = Vendors.FirstOrDefault(v => v.Id == CurrentPO.VendorId);
                        if (vendor == null)
                        {
                            // Load from DB if not in active vendors list
                            vendor = _dataService.GetVendors().FirstOrDefault(v => v.Id == CurrentPO.VendorId);
                        }
                        CurrentPO.Vendor = vendor;
                        if (vendor != null)
                        {
                            CurrentPO.VendorName = vendor.Name;
                            CurrentPO.VendorContact = vendor.Contact;
                            CurrentPO.VendorTel = vendor.Tel;
                            CurrentPO.VendorEmail = vendor.Email;
                            CurrentPO.VendorAddress = vendor.Address;
                        }
                    }
                    else
                    {
                        CurrentPO.Vendor = null;
                    }
                }
                if (e.PropertyName == nameof(PurchaseOrder.ProjectId))
                {
                    // Refresh PR list if project changes to ensure valid matching
                    LoadApprovedPRs();
                }
            };
        }


        [RelayCommand]
        private void SetRecommendedBidder(Bidder bidder)
        {
            if (bidder == null) return;

            foreach (var b in Bidders) b.IsWinner = false;
            bidder.IsWinner = true;

            CurrentBidAnalysis.RecommendedBidderId = bidder.Id;
            CurrentBidAnalysis.RecommendationReasons = bidder.Justification;

            // Ensure winner info is fully updated from Vendor if available
            if (bidder.VendorId.HasValue && bidder.VendorId != 0)
            {
                var vendor = Vendors.FirstOrDefault(v => v.Id == bidder.VendorId);
                if (vendor != null)
                {
                    if (string.IsNullOrWhiteSpace(bidder.Name)) bidder.Name = vendor.Name;
                    if (string.IsNullOrWhiteSpace(bidder.Address)) bidder.Address = vendor.Address;
                    if (string.IsNullOrWhiteSpace(bidder.Tel)) bidder.Tel = vendor.Tel;
                    if (string.IsNullOrWhiteSpace(bidder.Email)) bidder.Email = vendor.Email;
                    if (string.IsNullOrWhiteSpace(bidder.Contact)) bidder.Contact = vendor.Contact;
                }
            }

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

                // Select the newly created RFQ
                SelectedRFQ = RFQs.FirstOrDefault(r => r.RFQNumber == CurrentRFQ.RFQNumber);

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

        private void Bidder_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender is Bidder bidder && (e.PropertyName == nameof(Bidder.VendorId) || e.PropertyName == "VendorId") && bidder.VendorId.HasValue)
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
        }

        [RelayCommand]
        private void AddBidder()
        {
            var bidderNumber = Bidders.Count + 1;
            var bidder = new Bidder { Name = $"Bidder {bidderNumber}", BidAnalysisId = CurrentBidAnalysis.Id };
            bidder.PropertyChanged += Bidder_PropertyChanged;
            
            if (SelectedPR != null)
            {
                // If it's the first bidder, initialize matrix rows
                bool isFirst = Bidders.Count == 0;
                
                foreach(var item in SelectedPR.Items)
                {
                    var bidItem = new BidItem { BudgetLineId = item.BudgetLineId, Description = item.Description, Quantity = item.Quantity, Unit = item.Unit };
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

            try
            {
                CurrentBidAnalysis.Bidders = new ObservableCollection<Bidder>(Bidders.ToList());
                // Sync TotalAmount for DB
                foreach (var bidder in CurrentBidAnalysis.Bidders)
                {
                    bidder.TotalAmount = bidder.CalculatedTotal;
                }

                _dataService.SaveBidAnalysis(CurrentBidAnalysis);

                // If the winner was selected before save (ID was 0), re-sync RecommendedBidderId
                var winningBidder = CurrentBidAnalysis.Bidders.FirstOrDefault(b => b.IsWinner);
                if (winningBidder != null && (CurrentBidAnalysis.RecommendedBidderId == null || CurrentBidAnalysis.RecommendedBidderId == 0 || CurrentBidAnalysis.RecommendedBidderId != winningBidder.Id))
                {
                    CurrentBidAnalysis.RecommendedBidderId = winningBidder.Id;
                    // Double save to persist the RecommendedBidderId after we have the real database Id for the bidder
                    _dataService.SaveBidAnalysis(CurrentBidAnalysis);
                }

                int currentId = CurrentBidAnalysis.Id;
                LoadBidAnalyses();

                // Re-select to refresh UI and ensure IDs are synced
                var reloaded = BidAnalyses.FirstOrDefault(b => b.Id == currentId);
                if (reloaded != null) SelectedBidAnalysis = reloaded;

                MessageBox.Show("Bid Analysis Saved Successfully");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving Bid Analysis: {ex.Message}");
            }
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

            try
            {
                if (SkipBidAnalysis && (CurrentPO.VendorId == null || CurrentPO.VendorId == 0))
                {
                    MessageBox.Show("Please select a Vendor for this direct purchase.");
                    return;
                }

                if (!SkipBidAnalysis && SelectedBidAnalysis != null)
                {
                    var winner = SelectedBidAnalysis.Bidders.FirstOrDefault(b => b.IsWinner || b.Id == SelectedBidAnalysis.RecommendedBidderId);
                    if (winner == null)
                    {
                        MessageBox.Show("Please award a winner in the Bid Analysis before creating a PO.");
                        return;
                    }
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

                var mainVM = Application.Current.MainWindow.DataContext as MainViewModel;
                var helper = new NumberingHelper(mainVM?.ConnectionString ?? "Data Source=jaahd.db");

                var currency = analysisToUse?.Currency ?? SelectedPR.Currency;
                var rate = analysisToUse?.ExchangeRate ?? SelectedPR.ExchangeRate;

                var newPO = new PurchaseOrder
                {
                    PRId = SelectedPR.Id,
                    ProjectId = SelectedPR.ProjectId,
                    BidAnalysisId = (SkipBidAnalysis || (analysisToUse != null && analysisToUse.Id == 0)) ? (int?)null : analysisToUse?.Id,
                    Date = DateTime.Now,
                    PONumber = helper.GenerateNumber("PO", SelectedPR.ProjectId),
                    Status = "Pending",
                    Terms = Settings.POTerms,
                    Currency = currency,
                    ExchangeRate = rate
                };

                if (SkipBidAnalysis)
                {
                    newPO.VendorId = CurrentPO.VendorId;
                    newPO.Vendor = Vendors.FirstOrDefault(v => v.Id == newPO.VendorId);
                    if (newPO.Vendor != null)
                    {
                        newPO.VendorName = newPO.Vendor.Name;
                        newPO.VendorContact = newPO.Vendor.Contact;
                        newPO.VendorTel = newPO.Vendor.Tel;
                        newPO.VendorEmail = newPO.Vendor.Email;
                        newPO.VendorAddress = newPO.Vendor.Address;
                    }
                    foreach(var item in SelectedPR.Items)
                    {
                        newPO.Items.Add(new POItem { BudgetLineId = item.BudgetLineId, Description = item.Description, Quantity = item.Quantity, Unit = item.Unit, UnitPrice = item.UnitPrice });
                    }
                }
                else if (analysisToUse != null)
                {
                    // Find winner by ID first, then by IsWinner flag
                    var winner = analysisToUse.Bidders.FirstOrDefault(b => b.Id > 0 && b.Id == (analysisToUse.RecommendedBidderId ?? 0));
                    if (winner == null) winner = analysisToUse.Bidders.FirstOrDefault(b => b.IsWinner);

                    if (winner != null)
                    {
                        // Handle potential currency conversion from Winner's items (which are usually in winner's currency)
                        // Actually, RFQ/BidAnalysis items usually match the PR items.
                        // We check if winner's items exist and copy them.
                        newPO.BidderId = winner.Id == 0 ? (int?)null : winner.Id;
                        newPO.VendorId = (winner.VendorId == null || winner.VendorId == 0) ? (int?)null : winner.VendorId;

                        // Priority 1: Master Vendor Record
                        var masterVendor = newPO.VendorId.HasValue ? Vendors.FirstOrDefault(v => v.Id == newPO.VendorId) : null;
                        if (masterVendor == null && newPO.VendorId.HasValue)
                            masterVendor = _dataService.GetVendors().FirstOrDefault(v => v.Id == newPO.VendorId);

                        // Priority 2: Specific Bidder Details (falling back to master if empty)
                        newPO.VendorName = !string.IsNullOrWhiteSpace(winner.Name) ? winner.Name : masterVendor?.Name;
                        newPO.VendorTel = !string.IsNullOrWhiteSpace(winner.Tel) ? winner.Tel : masterVendor?.Tel;
                        newPO.VendorEmail = !string.IsNullOrWhiteSpace(winner.Email) ? winner.Email : masterVendor?.Email;
                        newPO.VendorAddress = !string.IsNullOrWhiteSpace(winner.Address) ? winner.Address : masterVendor?.Address;
                        newPO.VendorContact = !string.IsNullOrWhiteSpace(winner.Contact) ? winner.Contact : masterVendor?.Contact;

                        // Hydrate Vendor object for UI binding
                        newPO.Vendor = masterVendor ?? new Vendor { Name = newPO.VendorName, Contact = newPO.VendorContact, Tel = newPO.VendorTel, Email = newPO.VendorEmail, Address = newPO.VendorAddress };

                        foreach(var item in winner.Items)
                        {
                            newPO.Items.Add(new POItem { BudgetLineId = item.BudgetLineId, Description = item.Description, Quantity = item.Quantity, Unit = item.Unit, UnitPrice = item.UnitPrice });
                        }
                    }
                    else
                    {
                        MessageBox.Show("The selected Bid Analysis does not have a Recommended Bidder set.");
                        return;
                    }
                }

                _dataService.SavePO(newPO);
                LoadPOs();

                // Select the newly created PO
                SelectedPO = POs.FirstOrDefault(p => p.PONumber == newPO.PONumber);

                MessageBox.Show("Purchase Order Created Successfully");
                LoadApprovedPRs();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating Purchase Order: {ex.Message}");
            }
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

            // Ensure winner is synced before approval
            var winner = Bidders.FirstOrDefault(b => b.IsWinner);
            if (winner != null && (CurrentBidAnalysis.RecommendedBidderId == null || CurrentBidAnalysis.RecommendedBidderId == 0))
            {
                CurrentBidAnalysis.RecommendedBidderId = winner.Id;
            }

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
            Projects = new ObservableCollection<Project>(_dataService.GetProjects());
            AllPRs = new ObservableCollection<PurchaseRequisition>(_dataService.GetPRs());
        }

        [RelayCommand]
        public void LoadApprovedPRs()
        {
            var all = _dataService.GetPRs().ToList();
            AllPRs = new ObservableCollection<PurchaseRequisition>(all);

            var prs = all.Where(p => p.Status == "FinalApproved").ToList();
            ApprovedPRs = new ObservableCollection<PurchaseRequisition>(prs);
        }

        [RelayCommand]
        public void LoadRFQs()
        {
            var items = _dataService.GetRFQs().ToList();
            RFQs.Clear();
            foreach (var item in items) RFQs.Add(item);
        }

        [RelayCommand]
        public void LoadBidAnalyses()
        {
            var items = _dataService.GetBidAnalyses().ToList();
            BidAnalyses.Clear();
            foreach (var item in items) BidAnalyses.Add(item);
        }

        [RelayCommand]
        public void LoadPOs()
        {
            var items = _dataService.GetPOs().ToList();
            POs.Clear();
            foreach (var item in items) POs.Add(item);
        }

        [RelayCommand]
        private void SavePO()
        {
            if (CurrentPO == null) return;

            try
            {
                // Ensure vendor is loaded if VendorId exists
                if (CurrentPO.VendorId.HasValue && CurrentPO.VendorId > 0)
                {
                    CurrentPO.Vendor = Vendors.FirstOrDefault(v => v.Id == CurrentPO.VendorId);
                }

                _dataService.SavePO(CurrentPO);
                LoadPOs();

                // Re-select to refresh UI
                var reloaded = POs.FirstOrDefault(p => p.Id == CurrentPO.Id);
                if (reloaded != null) SelectedPO = reloaded;

                MessageBox.Show("Purchase Order Saved Successfully");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving Purchase Order: {ex.Message}\n\nPlease ensure a valid Project and PR are selected.", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

                // Re-attach PropertyChanged handlers for bidders to handle VendorId selection
                foreach(var bidder in Bidders)
                {
                    bidder.PropertyChanged -= Bidder_PropertyChanged; // Prevent multiple subscriptions
                    bidder.PropertyChanged += Bidder_PropertyChanged;
                }

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
                if (CurrentPO.VendorId.HasValue && CurrentPO.VendorId > 0 && CurrentPO.Vendor == null)
                {
                    var vendor = Vendors.FirstOrDefault(v => v.Id == CurrentPO.VendorId);
                    if (vendor == null)
                    {
                        vendor = _dataService.GetVendors().FirstOrDefault(v => v.Id == CurrentPO.VendorId);
                    }
                    CurrentPO.Vendor = vendor;
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
