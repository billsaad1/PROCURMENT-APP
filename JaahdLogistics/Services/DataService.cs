using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using Dapper;
using JaahdLogistics.Models;
using System.Collections.ObjectModel;

namespace JaahdLogistics.Services
{
    public class DataService : IDataService
    {
        private readonly string _connectionString;

        public DataService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public User? Authenticate(string username, string password)
        {
            using var connection = new SqliteConnection(_connectionString);
            var user = connection.QuerySingleOrDefault<User>(
                "SELECT * FROM Users WHERE Username = @username",
                new { username });

            if (user != null && JaahdLogistics.Helpers.SecurityHelper.VerifyPassword(password, user.PasswordHash))
            {
                return user;
            }
            return null;
        }

        public IEnumerable<User> GetUsers()
        {
            using var connection = new SqliteConnection(_connectionString);
            return connection.Query<User>("SELECT * FROM Users");
        }

        public void SaveUser(User user)
        {
            using var connection = new SqliteConnection(_connectionString);
            if (user.Id == 0)
            {
                // If the user doesn't have a hash set, it's a new user from settings, set a default password
                if (string.IsNullOrEmpty(user.PasswordHash))
                {
                    user.PasswordHash = JaahdLogistics.Helpers.SecurityHelper.HashPassword("1234");
                }
                user.Id = connection.QuerySingle<int>(
                    "INSERT INTO Users (Username, PasswordHash, Role, FullName, Position, SignatureImage) " +
                    "VALUES (@Username, @PasswordHash, @Role, @FullName, @Position, @SignatureImage); SELECT last_insert_rowid();", user);
            }
            else
            {
                // Update everything EXCEPT PasswordHash unless it's explicitly provided
                if (!string.IsNullOrEmpty(user.PasswordHash) && !user.PasswordHash.StartsWith("$")) // Simple check if it's a new plaintext password
                {
                     user.PasswordHash = JaahdLogistics.Helpers.SecurityHelper.HashPassword(user.PasswordHash);
                     connection.Execute(
                        "UPDATE Users SET Username=@Username, PasswordHash=@PasswordHash, Role=@Role, FullName=@FullName, " +
                        "Position=@Position, SignatureImage=@SignatureImage WHERE Id=@Id", user);
                }
                else
                {
                    connection.Execute(
                        "UPDATE Users SET Username=@Username, Role=@Role, FullName=@FullName, " +
                        "Position=@Position, SignatureImage=@SignatureImage WHERE Id=@Id", user);
                }
            }
        }

        public void DeleteUser(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Execute("DELETE FROM Users WHERE Id = @id", new { id });
        }

        public IEnumerable<Project> GetProjects()
        {
            using var connection = new SqliteConnection(_connectionString);
            return connection.Query<Project>("SELECT * FROM Projects");
        }

        public void SaveProject(Project project)
        {
            using var connection = new SqliteConnection(_connectionString);
            if (project.Id == 0)
            {
                project.Id = connection.QuerySingle<int>(
                    "INSERT INTO Projects (Name, Code, Year) VALUES (@Name, @Code, @Year); SELECT last_insert_rowid();", project);
            }
            else
            {
                connection.Execute("UPDATE Projects SET Name=@Name, Code=@Code, Year=@Year WHERE Id=@Id", project);
            }
        }

        public void DeleteProject(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try {
                // Cascading delete for demo purposes.
                // In production, you'd usually restrict deletion of projects with active procurement.
                connection.Execute("DELETE FROM PRItems WHERE PRId IN (SELECT Id FROM PurchaseRequisitions WHERE ProjectId = @id)", new { id }, transaction);
                connection.Execute("DELETE FROM PurchaseRequisitions WHERE ProjectId = @id", new { id }, transaction);
                connection.Execute("DELETE FROM BudgetLines WHERE ProjectId = @id", new { id }, transaction);
                connection.Execute("DELETE FROM Projects WHERE Id = @id", new { id }, transaction);
                transaction.Commit();
            } catch (SqliteException ex) when (ex.SqliteErrorCode == 19) {
                transaction.Rollback();
                throw new Exception("Cannot delete project because it is referenced in complex procurement documents (POs, GRNs). Delete those first.");
            } catch {
                transaction.Rollback();
                throw;
            }
        }

        public void DeleteRFQ(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            try { connection.Execute("DELETE FROM RFQs WHERE Id = @id", new { id }); }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19) { throw new Exception("Cannot delete RFQ because it is referenced in a Bid Analysis."); }
        }

        public void DeleteBidAnalysis(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                connection.Execute("DELETE FROM BidItems WHERE BidderId IN (SELECT Id FROM Bidders WHERE BidAnalysisId = @id)", new { id }, transaction);
                connection.Execute("DELETE FROM Bidders WHERE BidAnalysisId = @id", new { id }, transaction);
                connection.Execute("DELETE FROM BidAnalyses WHERE Id = @id", new { id }, transaction);
                transaction.Commit();
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19) { transaction.Rollback(); throw new Exception("Cannot delete Bid Analysis because it is referenced in a PO."); }
            catch { transaction.Rollback(); throw; }
        }

        public void DeletePO(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                connection.Execute("DELETE FROM POItems WHERE POId = @id", new { id }, transaction);
                connection.Execute("DELETE FROM PurchaseOrders WHERE Id = @id", new { id }, transaction);
                transaction.Commit();
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19) { transaction.Rollback(); throw new Exception("Cannot delete PO because it is referenced in a GRN."); }
            catch { transaction.Rollback(); throw; }
        }

        public IEnumerable<PurchaseRequisition> GetPRs()
        {
            using var connection = new SqliteConnection(_connectionString);
            var prs = connection.Query<PurchaseRequisition>("SELECT * FROM PurchaseRequisitions").ToList();
            foreach (var pr in prs)
            {
                var items = connection.Query<PRItem>("SELECT * FROM PRItems WHERE PRId = @Id", new { pr.Id }).ToList();
                pr.Items.Clear();
                foreach (var item in items)
                {
                    pr.Items.Add(item);
                }
            }
            return prs;
        }

        public void DeletePR(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                connection.Execute("DELETE FROM PRItems WHERE PRId = @id", new { id }, transaction);
                connection.Execute("DELETE FROM PurchaseRequisitions WHERE Id = @id", new { id }, transaction);
                transaction.Commit();
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                transaction.Rollback();
                throw new Exception("Cannot delete PR because it is referenced in an RFQ or PO. Delete those first.");
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public IEnumerable<PurchaseOrder> GetPOs()
        {
            using var connection = new SqliteConnection(_connectionString);
            var pos = connection.Query<PurchaseOrder>("SELECT * FROM PurchaseOrders").ToList();
            foreach (var po in pos)
            {
                var items = connection.Query<POItem>("SELECT * FROM POItems WHERE POId = @Id", new { po.Id }).ToList();
                po.Items = new ObservableCollection<POItem>(items);

                // Load Approvals for PO
                var approvals = GetApprovals("PO", po.Id);
                foreach (var app in approvals)
                {
                    if (app.Status == "CheckedByLogistics") { po.LogisticsSignature = app.SignatureImage; po.LogisticsName = app.FullName; }
                    if (app.Status == "ReviewedByFinance") { po.FinanceSignature = app.SignatureImage; po.FinanceName = app.FullName; }
                    if (app.Status == "ApprovedByPM") { po.PMSignature = app.SignatureImage; po.PMName = app.FullName; }
                    if (app.Status == "FinalApproved") { po.FinalSignature = app.SignatureImage; po.FinalName = app.FullName; }
                }
            }
            return pos;
        }

        public IEnumerable<RFQ> GetRFQs()
        {
            using var connection = new SqliteConnection(_connectionString);
            return connection.Query<RFQ>("SELECT * FROM RFQs");
        }

        public IEnumerable<BidAnalysis> GetBidAnalyses()
        {
            using var connection = new SqliteConnection(_connectionString);
            var analyses = connection.Query<BidAnalysis>("SELECT * FROM BidAnalyses").ToList();
            foreach (var analysis in analyses)
            {
                var bidders = connection.Query<Bidder>("SELECT * FROM Bidders WHERE BidAnalysisId = @Id", new { analysis.Id }).ToList();
                analysis.Bidders = new ObservableCollection<Bidder>(bidders);
                foreach (var bidder in analysis.Bidders)
                {
                    var items = connection.Query<BidItem>("SELECT * FROM BidItems WHERE BidderId = @Id", new { bidder.Id }).ToList();
                    bidder.Items = new ObservableCollection<BidItem>(items);
                }
            }
            return analyses;
        }

        public void SaveRFQ(RFQ rfq)
        {
            using var connection = new SqliteConnection(_connectionString);
            if (rfq.Id == 0)
            {
                rfq.Id = connection.QuerySingle<int>(
                    "INSERT INTO RFQs (RFQNumber, PRId, Date, ClosingDate, Terms) " +
                    "VALUES (@RFQNumber, @PRId, @Date, @ClosingDate, @Terms); SELECT last_insert_rowid();", rfq);
            }
            else
            {
                connection.Execute(
                    "UPDATE RFQs SET RFQNumber=@RFQNumber, ClosingDate=@ClosingDate, Terms=@Terms WHERE Id=@Id", rfq);
            }
        }

        public void SaveBidAnalysis(BidAnalysis analysis)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                if (analysis.Id == 0)
                {
                    analysis.Id = connection.QuerySingle<int>(
                        "INSERT INTO BidAnalyses (RFQId, Date, RecommendedBidderId, Justification, Status) " +
                        "VALUES (@RFQId, @Date, @RecommendedBidderId, @Justification, @Status); SELECT last_insert_rowid();",
                        analysis, transaction);
                }
                else
                {
                    connection.Execute(
                        "UPDATE BidAnalyses SET RecommendedBidderId=@RecommendedBidderId, Justification=@Justification, Status=@Status WHERE Id=@Id",
                        analysis, transaction);
                    connection.Execute("DELETE FROM BidItems WHERE BidderId IN (SELECT Id FROM Bidders WHERE BidAnalysisId = @Id)", new { analysis.Id }, transaction);
                    connection.Execute("DELETE FROM Bidders WHERE BidAnalysisId = @Id", new { analysis.Id }, transaction);
                }

                foreach (var bidder in analysis.Bidders)
                {
                    bidder.BidAnalysisId = analysis.Id;
                    bidder.Id = connection.QuerySingle<int>(
                        "INSERT INTO Bidders (BidAnalysisId, Name, Address, Contact) VALUES (@BidAnalysisId, @Name, @Address, @Contact); SELECT last_insert_rowid();",
                        bidder, transaction);

                    foreach (var item in bidder.Items)
                    {
                        item.BidderId = bidder.Id;
                        connection.Execute(
                            "INSERT INTO BidItems (BidderId, Description, Unit, Quantity, UnitPrice) VALUES (@BidderId, @Description, @Unit, @Quantity, @UnitPrice)",
                            item, transaction);
                    }
                }
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public void SavePO(PurchaseOrder po)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                if (po.Id == 0)
                {
                    po.Id = connection.QuerySingle<int>(
                        "INSERT INTO PurchaseOrders (PONumber, PRId, ProjectId, BidAnalysisId, VendorId, Date, Terms, Status) " +
                        "VALUES (@PONumber, @PRId, @ProjectId, @BidAnalysisId, @VendorId, @Date, @Terms, @Status); SELECT last_insert_rowid();",
                        po, transaction);
                }
                else
                {
                    connection.Execute(
                        "UPDATE PurchaseOrders SET PONumber=@PONumber, Terms=@Terms, Status=@Status WHERE Id=@Id",
                        po, transaction);
                    connection.Execute("DELETE FROM POItems WHERE POId = @Id", new { po.Id }, transaction);
                }

                foreach (var item in po.Items)
                {
                    item.POId = po.Id;
                    connection.Execute(
                        "INSERT INTO POItems (POId, Description, Unit, Quantity, UnitPrice) VALUES (@POId, @Description, @Unit, @Quantity, @UnitPrice)",
                        item, transaction);
                }
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public void SavePR(PurchaseRequisition pr)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                if (pr.Id == 0)
                {
                    pr.Id = connection.QuerySingle<int>(
                        "INSERT INTO PurchaseRequisitions (PRNumber, ProjectId, RequesterId, Date, Justification, Currency, ExchangeRate, Status) " +
                        "VALUES (@PRNumber, @ProjectId, @RequesterId, @Date, @Justification, @Currency, @ExchangeRate, @Status); SELECT last_insert_rowid();",
                        pr, transaction);
                }
                else
                {
                    connection.Execute(
                        "UPDATE PurchaseRequisitions SET PRNumber=@PRNumber, ProjectId=@ProjectId, Justification=@Justification, Status=@Status, Currency=@Currency, ExchangeRate=@ExchangeRate WHERE Id=@Id",
                        pr, transaction);

                    try {
                        connection.Execute("DELETE FROM PRItems WHERE PRId = @Id", new { pr.Id }, transaction);
                    } catch (SqliteException ex) when (ex.SqliteErrorCode == 19) {
                         throw new Exception("Cannot update PR items because they are already referenced in subsequent documents.");
                    }
                }

                foreach (var item in pr.Items)
                {
                    item.PRId = pr.Id;
                    connection.Execute(
                        "INSERT INTO PRItems (PRId, BudgetLineId, Description, Unit, Quantity, UnitPrice) " +
                        "VALUES (@PRId, @BudgetLineId, @Description, @Unit, @Quantity, @UnitPrice)", item, transaction);
                }
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public void SaveBudgetLine(BudgetLine budgetLine)
        {
            using var connection = new SqliteConnection(_connectionString);
            if (budgetLine.Id == 0)
            {
                budgetLine.Id = connection.QuerySingle<int>(
                    "INSERT INTO BudgetLines (ProjectId, Code, Name, Description, Unit, Quantity, UnitPrice, TotalAmount, Currency) " +
                    "VALUES (@ProjectId, @Code, @Name, @Description, @Unit, @Quantity, @UnitPrice, @TotalAmount, @Currency); SELECT last_insert_rowid();", budgetLine);
            }
            else
            {
                connection.Execute(
                    "UPDATE BudgetLines SET Code=@Code, Name=@Name, Description=@Description, Unit=@Unit, " +
                    "Quantity=@Quantity, UnitPrice=@UnitPrice, TotalAmount=@TotalAmount, Currency=@Currency WHERE Id=@Id", budgetLine);
            }
        }

        public void DeleteBudgetLine(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Execute("DELETE FROM BudgetLines WHERE Id = @id", new { id });
        }

        public IEnumerable<BudgetLine> GetBudgetLines(int projectId)
        {
            using var connection = new SqliteConnection(_connectionString);
            return connection.Query<BudgetLine>("SELECT * FROM BudgetLines WHERE ProjectId = @projectId", new { projectId });
        }

        public Settings GetSettings()
        {
            using var connection = new SqliteConnection(_connectionString);
            return connection.QuerySingle<Settings>("SELECT * FROM Settings WHERE Id = 1");
        }

        public void SaveSettings(Settings settings)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Execute(
                "UPDATE Settings SET AssociationName=@AssociationName, Address=@Address, " +
                "ContactInfo=@ContactInfo, LogoImage=@LogoImage, " +
                "PRTerms=@PRTerms, RFQTerms=@RFQTerms, POTerms=@POTerms WHERE Id=1", settings);
        }

        public decimal GetSpentBudget(int budgetLineId)
        {
            using var connection = new SqliteConnection(_connectionString);
            var budgetLine = connection.QuerySingleOrDefault<BudgetLine>("SELECT * FROM BudgetLines WHERE Id = @budgetLineId", new { budgetLineId });
            if (budgetLine == null) return 0;

            var prItems = connection.Query<dynamic>(
                "SELECT pi.Quantity, pi.UnitPrice, pr.Currency, pr.ExchangeRate " +
                "FROM PRItems pi JOIN PurchaseRequisitions pr ON pi.PRId = pr.Id " +
                "WHERE pi.BudgetLineId = @budgetLineId AND pr.Status != 'Rejected'", new { budgetLineId });

            decimal totalSpentInBudgetCurrency = 0;
            foreach (var item in prItems)
            {
                decimal quantity = Convert.ToDecimal(item.Quantity);
                decimal unitPrice = Convert.ToDecimal(item.UnitPrice);
                decimal exchangeRate = Convert.ToDecimal(item.ExchangeRate ?? 1.0);
                decimal itemTotal = quantity * unitPrice;

                if (item.Currency == budgetLine.Currency)
                {
                    totalSpentInBudgetCurrency += itemTotal;
                }
                else if (item.Currency == "YER" && budgetLine.Currency == "USD" && exchangeRate > 0)
                {
                    totalSpentInBudgetCurrency += itemTotal / exchangeRate;
                }
                else if (item.Currency == "USD" && budgetLine.Currency == "YER")
                {
                    totalSpentInBudgetCurrency += itemTotal * exchangeRate;
                }
            }
            return totalSpentInBudgetCurrency;
        }

        public decimal GetRemainingBudget(int budgetLineId)
        {
            using var connection = new SqliteConnection(_connectionString);
            var budgetLine = connection.QuerySingleOrDefault<BudgetLine>("SELECT * FROM BudgetLines WHERE Id = @budgetLineId", new { budgetLineId });
            if (budgetLine == null) return 0;

            return budgetLine.TotalAmount - GetSpentBudget(budgetLineId);
        }

        public decimal GetLastExchangeRate(string currency)
        {
            using var connection = new SqliteConnection(_connectionString);
            return connection.QueryFirstOrDefault<decimal>(
                "SELECT ExchangeRate FROM PurchaseRequisitions WHERE Currency = @currency ORDER BY Date DESC LIMIT 1",
                new { currency });
        }

        public void ApproveEntity(string entityType, int entityId, int userId, string status)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Execute(
                "INSERT INTO Approvals (EntityType, EntityId, UserId, Status) VALUES (@entityType, @entityId, @userId, @status)",
                new { entityType, entityId, userId, status });
        }

        public IEnumerable<dynamic> GetApprovals(string entityType, int entityId)
        {
            using var connection = new SqliteConnection(_connectionString);
            return connection.Query<dynamic>(
                "SELECT a.*, u.FullName, u.Position, u.SignatureImage FROM Approvals a JOIN Users u ON a.UserId = u.Id " +
                "WHERE a.EntityType = @entityType AND a.EntityId = @entityId", new { entityType, entityId });
        }

        public void SaveGRN(GoodsReceivingNotes grn, List<GRNItems> items)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try {
                var grnId = connection.QuerySingle<int>(
                    "INSERT INTO GoodsReceivingNotes (GRNNumber, POId, ReceiverId) VALUES (@GRNNumber, @POId, @ReceiverId); SELECT last_insert_rowid();",
                    grn, transaction);

                foreach(var item in items) {
                    item.GRNId = grnId;
                    connection.Execute(
                        "INSERT INTO GRNItems (GRNId, POItemId, ReceivedQuantity, AcceptedQuantity, RejectedQuantity, RejectReason) " +
                        "VALUES (@GRNId, @POItemId, @ReceivedQuantity, @AcceptedQuantity, @RejectedQuantity, @RejectReason)",
                        item, transaction);

                    var poItem = connection.QuerySingle<POItem>("SELECT * FROM POItems WHERE Id = @POItemId", new { item.POItemId }, transaction);
                    var po = connection.QuerySingle<PurchaseOrder>("SELECT * FROM PurchaseOrders WHERE Id = @POId", new { POId = poItem.POId }, transaction);

                    var existing = connection.QuerySingleOrDefault<int?>(
                        "SELECT Id FROM Inventory WHERE ItemDescription = @Description AND ProjectId = @ProjectId",
                        new { Description = poItem.Description, ProjectId = po.ProjectId }, transaction);

                    if (existing.HasValue) {
                        connection.Execute(
                            "UPDATE Inventory SET CurrentQuantity = CurrentQuantity + @AcceptedQuantity WHERE Id = @Id",
                            new { AcceptedQuantity = item.AcceptedQuantity, Id = existing.Value }, transaction);
                    } else {
                        connection.Execute(
                            "INSERT INTO Inventory (ItemDescription, ProjectId, CurrentQuantity, Unit) VALUES (@Description, @ProjectId, @AcceptedQuantity, @Unit)",
                            new { Description = poItem.Description, ProjectId = po.ProjectId, AcceptedQuantity = item.AcceptedQuantity, Unit = poItem.Unit }, transaction);
                    }
                }
                transaction.Commit();
            } catch {
                transaction.Rollback();
                throw;
            }
        }

        public IEnumerable<GoodsReceivingNotes> GetGRNs()
        {
            using var connection = new SqliteConnection(_connectionString);
            var grns = connection.Query<GoodsReceivingNotes>("SELECT * FROM GoodsReceivingNotes").ToList();
            foreach (var grn in grns)
            {
                var items = connection.Query<GRNItems>("SELECT * FROM GRNItems WHERE GRNId = @Id", new { grn.Id }).ToList();
                grn.Items = items;
            }
            return grns;
        }

        public void DeleteGRN(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                // Decrement inventory before deleting GRN
                var items = connection.Query<GRNItems>("SELECT * FROM GRNItems WHERE GRNId = @id", new { id }, transaction);
                var grn = connection.QuerySingle<GoodsReceivingNotes>("SELECT * FROM GoodsReceivingNotes WHERE Id = @id", new { id }, transaction);
                var po = connection.QuerySingle<PurchaseOrder>("SELECT * FROM PurchaseOrders WHERE Id = @POId", new { POId = grn.POId }, transaction);

                foreach (var item in items)
                {
                    var poItem = connection.QuerySingle<POItem>("SELECT * FROM POItems WHERE Id = @POItemId", new { item.POItemId }, transaction);
                    connection.Execute(
                        "UPDATE Inventory SET CurrentQuantity = CurrentQuantity - @AcceptedQuantity WHERE ItemDescription = @Description AND ProjectId = @ProjectId",
                        new { AcceptedQuantity = item.AcceptedQuantity, Description = poItem.Description, ProjectId = po.ProjectId }, transaction);
                }

                connection.Execute("DELETE FROM GRNItems WHERE GRNId = @id", new { id }, transaction);
                connection.Execute("DELETE FROM GoodsReceivingNotes WHERE Id = @id", new { id }, transaction);
                transaction.Commit();
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19) { transaction.Rollback(); throw new Exception("Cannot delete GRN because it is referenced in a Three-Way Match."); }
            catch { transaction.Rollback(); throw; }
        }

        public void SaveThreeWayMatch(ThreeWayMatch match)
        {
            using var connection = new SqliteConnection(_connectionString);
            if (match.Id == 0)
            {
                connection.Execute(
                    "INSERT INTO ThreeWayMatch (POId, GRNId, InvoiceNumber, InvoiceDetails, InvoiceScan, Date, Status) " +
                    "VALUES (@POId, @GRNId, @InvoiceNumber, @InvoiceDetails, @InvoiceScan, @Date, @Status)", match);
            }
            else
            {
                connection.Execute(
                    "UPDATE ThreeWayMatch SET POId=@POId, GRNId=@GRNId, InvoiceNumber=@InvoiceNumber, " +
                    "InvoiceDetails=@InvoiceDetails, InvoiceScan=@InvoiceScan, Status=@Status WHERE Id=@Id", match);
            }
        }

        public void DeleteThreeWayMatch(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Execute("DELETE FROM ThreeWayMatch WHERE Id = @id", new { id });
        }

        public IEnumerable<ThreeWayMatch> GetThreeWayMatches()
        {
            using var connection = new SqliteConnection(_connectionString);
            return connection.Query<ThreeWayMatch>("SELECT * FROM ThreeWayMatch");
        }

        public IEnumerable<string> GetPreviousItemDescriptions()
        {
            using var connection = new SqliteConnection(_connectionString);
            return connection.Query<string>("SELECT DISTINCT Description FROM PRItems UNION SELECT DISTINCT Description FROM POItems");
        }
    }
}
