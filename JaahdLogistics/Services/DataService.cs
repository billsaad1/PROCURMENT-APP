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
                connection.Execute(
                    "INSERT INTO Users (Username, PasswordHash, Role, FullName, Position, SignatureImage) " +
                    "VALUES (@Username, @PasswordHash, @Role, @FullName, @Position, @SignatureImage)", user);
            }
            else
            {
                connection.Execute(
                    "UPDATE Users SET Username=@Username, Role=@Role, FullName=@FullName, " +
                    "Position=@Position, SignatureImage=@SignatureImage WHERE Id=@Id", user);
            }
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
                connection.Execute("INSERT INTO Projects (Name, Code, Year) VALUES (@Name, @Code, @Year)", project);
            }
            else
            {
                connection.Execute("UPDATE Projects SET Name=@Name, Code=@Code, Year=@Year WHERE Id=@Id", project);
            }
        }

        public IEnumerable<PurchaseRequisition> GetPRs()
        {
            using var connection = new SqliteConnection(_connectionString);
            var prs = connection.Query<PurchaseRequisition>("SELECT * FROM PurchaseRequisitions").ToList();
            foreach (var pr in prs)
            {
                var items = connection.Query<PRItem>("SELECT * FROM PRItems WHERE PRId = @Id", new { pr.Id }).ToList();
                pr.Items = new ObservableCollection<PRItem>(items);
            }
            return prs;
        }

        public IEnumerable<PurchaseOrder> GetPOs()
        {
            using var connection = new SqliteConnection(_connectionString);
            var pos = connection.Query<PurchaseOrder>("SELECT * FROM PurchaseOrders").ToList();
            foreach (var po in pos)
            {
                var items = connection.Query<POItem>("SELECT * FROM POItems WHERE POId = @Id", new { po.Id }).ToList();
                po.Items = new ObservableCollection<POItem>(items);
            }
            return pos;
        }

        public void SaveRFQ(RFQ rfq)
        {
            using var connection = new SqliteConnection(_connectionString);
            if (rfq.Id == 0)
            {
                connection.Execute(
                    "INSERT INTO RFQs (RFQNumber, PRId, Date, ClosingDate, Terms) " +
                    "VALUES (@RFQNumber, @PRId, @Date, @ClosingDate, @Terms)", rfq);
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
                        "INSERT INTO PurchaseOrders (PONumber, PRId, BidAnalysisId, VendorId, Date, Terms, Status) " +
                        "VALUES (@PONumber, @PRId, @BidAnalysisId, @VendorId, @Date, @Terms, @Status); SELECT last_insert_rowid();",
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
                        "UPDATE PurchaseRequisitions SET PRNumber=@PRNumber, ProjectId=@ProjectId, Justification=@Justification, Status=@Status WHERE Id=@Id",
                        pr, transaction);
                    connection.Execute("DELETE FROM PRItems WHERE PRId = @Id", new { pr.Id }, transaction);
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
                connection.Execute(
                    "INSERT INTO BudgetLines (ProjectId, Code, Description, TotalAmount, Currency) " +
                    "VALUES (@ProjectId, @Code, @Description, @TotalAmount, @Currency)", budgetLine);
            }
            else
            {
                connection.Execute(
                    "UPDATE BudgetLines SET Code=@Code, Description=@Description, " +
                    "TotalAmount=@TotalAmount, Currency=@Currency WHERE Id=@Id", budgetLine);
            }
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
                "UPDATE Settings SET AssociationName=@AssociationName, LogoImage=@LogoImage, " +
                "PRTerms=@PRTerms, RFQTerms=@RFQTerms, POTerms=@POTerms WHERE Id=1", settings);
        }

        public decimal GetRemainingBudget(int budgetLineId)
        {
            using var connection = new SqliteConnection(_connectionString);
            var budgetLine = connection.QuerySingle<BudgetLine>("SELECT * FROM BudgetLines WHERE Id = @budgetLineId", new { budgetLineId });

            var prItems = connection.Query<dynamic>(
                "SELECT pi.Quantity, pi.UnitPrice, pr.Currency, pr.ExchangeRate " +
                "FROM PRItems pi JOIN PurchaseRequisitions pr ON pi.PRId = pr.Id " +
                "WHERE pi.BudgetLineId = @budgetLineId", new { budgetLineId });

            decimal totalSpentInBudgetCurrency = 0;
            foreach (var item in prItems)
            {
                decimal quantity = Convert.ToDecimal(item.Quantity);
                decimal unitPrice = Convert.ToDecimal(item.UnitPrice);
                decimal exchangeRate = Convert.ToDecimal(item.ExchangeRate);
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

            return budgetLine.TotalAmount - totalSpentInBudgetCurrency;
        }

        public void ApproveEntity(string entityType, int entityId, int userId, string status)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Execute(
                "INSERT INTO Approvals (EntityType, EntityId, UserId, Status) VALUES (@entityType, @entityId, @userId, @status)",
                new { entityType, entityId, userId, status });
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
                            "INSERT INTO Inventory (ItemDescription, ProjectId, CurrentQuantity) VALUES (@Description, @ProjectId, @AcceptedQuantity)",
                            new { Description = poItem.Description, ProjectId = po.ProjectId, AcceptedQuantity = item.AcceptedQuantity }, transaction);
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
            return connection.Query<GoodsReceivingNotes>("SELECT * FROM GoodsReceivingNotes");
        }

        public void SaveThreeWayMatch(ThreeWayMatch match)
        {
            using var connection = new SqliteConnection(_connectionString);
            if (match.Id == 0)
            {
                connection.Execute(
                    "INSERT INTO ThreeWayMatch (POId, GRNId, InvoiceNumber, Date, Status) " +
                    "VALUES (@POId, @GRNId, @InvoiceNumber, @Date, @Status)", match);
            }
            else
            {
                connection.Execute(
                    "UPDATE ThreeWayMatch SET POId=@POId, GRNId=@GRNId, InvoiceNumber=@InvoiceNumber, Status=@Status WHERE Id=@Id", match);
            }
        }
    }
}
