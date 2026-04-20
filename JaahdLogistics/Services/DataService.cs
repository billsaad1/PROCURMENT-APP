using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using Dapper;
using JaahdLogistics.Models;

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
            // In a real app, use password hashing. For this project, we'll use a simple comparison
            // but we'll assume it's hashed in a real scenario.
            return connection.QuerySingleOrDefault<User>(
                "SELECT * FROM Users WHERE Username = @username AND PasswordHash = @password",
                new { username, password });
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
            return connection.Query<PurchaseRequisition>("SELECT * FROM PurchaseRequisitions");
        }

        public IEnumerable<PurchaseOrder> GetPOs()
        {
            using var connection = new SqliteConnection(_connectionString);
            return connection.Query<PurchaseOrder>("SELECT * FROM PurchaseOrders");
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
            // Implementation for complex BidAnalysis save with Bidders and Items
        }

        public void SavePO(PurchaseOrder po)
        {
            using var connection = new SqliteConnection(_connectionString);
            if (po.Id == 0)
            {
                connection.Execute(
                    "INSERT INTO PurchaseOrders (PONumber, PRId, BidAnalysisId, VendorId, Date, Terms, Status) " +
                    "VALUES (@PONumber, @PRId, @BidAnalysisId, @VendorId, @Date, @Terms, @Status)", po);
            }
            else
            {
                connection.Execute(
                    "UPDATE PurchaseOrders SET PONumber=@PONumber, Terms=@Terms, Status=@Status WHERE Id=@Id", po);
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
            var totalBudget = connection.ExecuteScalar<decimal>("SELECT TotalAmount FROM BudgetLines WHERE Id = @budgetLineId", new { budgetLineId });
            var spent = connection.ExecuteScalar<decimal>(
                "SELECT COALESCE(SUM(Quantity * UnitPrice), 0) FROM PRItems WHERE BudgetLineId = @budgetLineId", new { budgetLineId });
            return totalBudget - spent;
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
    }
}
