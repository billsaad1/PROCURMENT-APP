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
            connection.Execute("PRAGMA foreign_keys = ON;");
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
            connection.Execute("PRAGMA foreign_keys = ON;");
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
            connection.Execute("PRAGMA foreign_keys = ON;");
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
            connection.Execute("PRAGMA foreign_keys = ON;");
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
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                transaction.Rollback();
                var fkIssues = connection.Query("PRAGMA foreign_key_check");
                string detail = "";
                foreach(var issue in fkIssues) detail += $"\n- Table '{issue.table}' row {issue.rowid} references missing key in '{issue.parent}'";

                if (string.IsNullOrEmpty(detail))
                {
                    // If check returns nothing, it might be a RESTRICT violation on delete/update
                    throw new Exception("Database constraint violation (Foreign Key). Please ensure all linked documents exist and are not locked by downstream records.", ex);
                }
                throw new Exception($"Database integrity error: {detail}", ex);
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
                po.Items.Clear();
                foreach (var item in items) po.Items.Add(item);

                if (po.VendorId.HasValue && po.VendorId > 0)
                {
                    po.Vendor = connection.QuerySingleOrDefault<Vendor>("SELECT * FROM Vendors WHERE Id = @VendorId", new { po.VendorId });
                }
                else
                {
                    po.VendorId = null; // Clean up 0s
                }

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
                analysis.Bidders.Clear();
                foreach (var bidder in bidders)
                {
                    var items = connection.Query<BidItem>("SELECT * FROM BidItems WHERE BidderId = @Id", new { bidder.Id }).ToList();
                    bidder.Items.Clear();
                    foreach (var item in items) bidder.Items.Add(item);
                    analysis.Bidders.Add(bidder);
                }
            }
            return analyses;
        }

        public void SaveRFQ(RFQ rfq)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            connection.Execute("PRAGMA foreign_keys = ON;");
            try
            {
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
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                CheckForeignKeys(connection);
                throw new Exception($"Database constraint violation in RFQ: {ex.Message}", ex);
            }
        }

        public void SaveBidAnalysis(BidAnalysis analysis)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            connection.Execute("PRAGMA foreign_keys = ON;");
            using var transaction = connection.BeginTransaction();
            try
            {
                var analysisParam = new
                {
                    analysis.Id,
                    analysis.RFQId,
                    analysis.Date,
                    RecommendedBidderId = (analysis.RecommendedBidderId == 0) ? (int?)null : analysis.RecommendedBidderId,
                    analysis.Justification,
                    analysis.RecommendationReasons,
                    analysis.Status,
                    analysis.Currency,
                    analysis.ExchangeRate
                };

                if (analysis.Id == 0)
                {
                    analysis.Id = connection.QuerySingle<int>(
                        "INSERT INTO BidAnalyses (RFQId, Date, RecommendedBidderId, Justification, RecommendationReasons, Status, Currency, ExchangeRate) " +
                        "VALUES (@RFQId, @Date, @RecommendedBidderId, @Justification, @RecommendationReasons, @Status, @Currency, @ExchangeRate); SELECT last_insert_rowid();",
                        analysisParam, transaction);
                }
                else
                {
                    connection.Execute(
                        "UPDATE BidAnalyses SET RecommendedBidderId=@RecommendedBidderId, Justification=@Justification, RecommendationReasons=@RecommendationReasons, Status=@Status, Currency=@Currency, ExchangeRate=@ExchangeRate WHERE Id=@Id",
                        analysisParam, transaction);
                }

                var existingBiddersInDb = connection.Query<Bidder>("SELECT * FROM Bidders WHERE BidAnalysisId = @Id", new { analysis.Id }, transaction).ToList();
                var currentBidderIds = analysis.Bidders.Select(b => b.Id).ToList();

                // Delete bidders not in the list anymore
                foreach (var existing in existingBiddersInDb)
                {
                    if (!currentBidderIds.Contains(existing.Id))
                    {
                        try
                        {
                            connection.Execute("DELETE FROM BidItems WHERE BidderId = @Id", new { existing.Id }, transaction);
                            connection.Execute("DELETE FROM Bidders WHERE Id = @Id", new { existing.Id }, transaction);
                        }
                        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
                        {
                            // Referenced, can't delete
                        }
                    }
                }

                foreach (var bidder in analysis.Bidders)
                {
                    bidder.BidAnalysisId = analysis.Id;
                    var bidderParam = new
                    {
                        bidder.Id,
                        bidder.BidAnalysisId,
                        VendorId = (bidder.VendorId == 0) ? (int?)null : bidder.VendorId,
                        bidder.Name,
                        bidder.Address,
                        bidder.Contact,
                        bidder.Tel,
                        bidder.Email,
                        bidder.Justification,
                        bidder.IsWinner,
                        bidder.Discount,
                        bidder.MiscCosts,
                        bidder.TotalAmount,
                        bidder.QuoteScan
                    };

                    if (bidder.Id == 0)
                    {
                        bidder.Id = connection.QuerySingle<int>(
                            "INSERT INTO Bidders (BidAnalysisId, VendorId, Name, Address, Contact, Tel, Email, Justification, IsWinner, Discount, MiscCosts, TotalAmount, QuoteScan) " +
                            "VALUES (@BidAnalysisId, @VendorId, @Name, @Address, @Contact, @Tel, @Email, @Justification, @IsWinner, @Discount, @MiscCosts, @TotalAmount, @QuoteScan); SELECT last_insert_rowid();",
                            bidderParam, transaction);
                    }
                    else
                    {
                        connection.Execute(
                            "UPDATE Bidders SET VendorId=@VendorId, Name=@Name, Address=@Address, Contact=@Contact, Tel=@Tel, Email=@Email, Justification=@Justification, IsWinner=@IsWinner, Discount=@Discount, MiscCosts=@MiscCosts, TotalAmount=@TotalAmount, QuoteScan=@QuoteScan WHERE Id=@Id",
                            bidderParam, transaction);
                    }

                    var existingItemsInDb = connection.Query<BidItem>("SELECT * FROM BidItems WHERE BidderId = @Id", new { bidder.Id }, transaction).ToList();
                    var currentItemIds = bidder.Items.Select(i => i.Id).ToList();

                    // Delete items not in list
                    foreach (var existing in existingItemsInDb)
                    {
                        if (!currentItemIds.Contains(existing.Id))
                        {
                            try { connection.Execute("DELETE FROM BidItems WHERE Id = @Id", new { existing.Id }, transaction); }
                            catch (SqliteException ex) when (ex.SqliteErrorCode == 19) { }
                        }
                    }

                    foreach (var item in bidder.Items)
                    {
                        item.BidderId = bidder.Id;
                        if (item.Id == 0)
                        {
                            item.Id = connection.QuerySingle<int>(
                            "INSERT INTO BidItems (BidderId, BudgetLineId, Description, Unit, Quantity, UnitPrice) VALUES (@BidderId, @BudgetLineId, @Description, @Unit, @Quantity, @UnitPrice); SELECT last_insert_rowid();",
                                item, transaction);
                        }
                        else
                        {
                            connection.Execute(
                            "UPDATE BidItems SET BudgetLineId=@BudgetLineId, Description=@Description, Unit=@Unit, Quantity=@Quantity, UnitPrice=@UnitPrice WHERE Id=@Id",
                                item, transaction);
                        }
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

        private void CheckForeignKeys(SqliteConnection connection)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "PRAGMA foreign_key_check;";
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                string table = reader.GetString(0);
                long rowid = reader.GetInt64(1);
                string parent = reader.GetString(2);
                int fkid = reader.GetInt32(3);
                throw new Exception($"Database Integrity Error: Table '{table}' (row {rowid}) has a broken link to '{parent}'. Please ensure all related records (Project, User, PR, etc.) exist.");
            }
        }

        public void SavePO(PurchaseOrder po)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            // 1. Mandatory Validations
            if (po.PRId <= 0) throw new Exception("Purchase Order must be linked to a valid Purchase Requisition (PRId is missing).");
            if (po.ProjectId <= 0) throw new Exception("Purchase Order must be linked to a valid Project (ProjectId is missing).");

            // 2. Database Existence Checks
            if (connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Projects WHERE Id = @ProjectId", new { po.ProjectId }) == 0)
                throw new Exception($"Project ID {po.ProjectId} does not exist in the database.");

            if (connection.ExecuteScalar<int>("SELECT COUNT(*) FROM PurchaseRequisitions WHERE Id = @PRId", new { po.PRId }) == 0)
                throw new Exception($"Purchase Requisition ID {po.PRId} does not exist in the database.");

            // 3. Optional Link Checks
            if (po.BidAnalysisId.HasValue && po.BidAnalysisId > 0)
                if (connection.ExecuteScalar<int>("SELECT COUNT(*) FROM BidAnalyses WHERE Id = @BidAnalysisId", new { po.BidAnalysisId }) == 0)
                    throw new Exception($"Bid Analysis ID {po.BidAnalysisId} does not exist.");

            if (po.BidderId.HasValue && po.BidderId > 0)
                if (connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Bidders WHERE Id = @BidderId", new { po.BidderId }) == 0)
                    throw new Exception($"Winning Bidder ID {po.BidderId} does not exist.");

            if (po.VendorId.HasValue && po.VendorId > 0)
                if (connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Vendors WHERE Id = @VendorId", new { po.VendorId }) == 0)
                    throw new Exception($"Vendor ID {po.VendorId} does not exist.");

            // 4. Item-Level Validations
            foreach (var item in po.Items)
            {
                if (item.BudgetLineId.HasValue && item.BudgetLineId > 0)
                {
                    if (connection.ExecuteScalar<int>("SELECT COUNT(*) FROM BudgetLines WHERE Id = @BudgetLineId", new { item.BudgetLineId }) == 0)
                        throw new Exception($"Budget Line ID {item.BudgetLineId} for item '{item.Description}' does not exist.");
                }
            }

            connection.Execute("PRAGMA foreign_keys = ON;");
            using var transaction = connection.BeginTransaction();

            // Explicitly map 0 to null for optional Foreign Keys to avoid SQLite Error 19
            var param = new {
                po.Id, po.PONumber, po.PRId, po.ProjectId,
                BidAnalysisId = (po.BidAnalysisId == null || po.BidAnalysisId == 0) ? (int?)null : po.BidAnalysisId,
                BidderId = (po.BidderId == null || po.BidderId == 0) ? (int?)null : po.BidderId,
                VendorId = (po.VendorId == null || po.VendorId == 0) ? (int?)null : po.VendorId,
                po.Date, po.Terms, po.Clause, po.Status, po.Currency, po.ExchangeRate,
                po.VendorName, po.VendorContact, po.VendorTel, po.VendorEmail, po.VendorAddress
            };

            try
            {
                if (po.Id == 0)
                {
                    po.Id = connection.QuerySingle<int>(
                        "INSERT INTO PurchaseOrders (PONumber, PRId, ProjectId, BidAnalysisId, BidderId, VendorId, Date, Terms, Clause, Status, Currency, ExchangeRate, VendorName, VendorContact, VendorTel, VendorEmail, VendorAddress) " +
                        "VALUES (@PONumber, @PRId, @ProjectId, @BidAnalysisId, @BidderId, @VendorId, @Date, @Terms, @Clause, @Status, @Currency, @ExchangeRate, @VendorName, @VendorContact, @VendorTel, @VendorEmail, @VendorAddress); SELECT last_insert_rowid();",
                        param, transaction);
                }
                else
                {
                    connection.Execute(
                        "UPDATE PurchaseOrders SET PONumber=@PONumber, PRId=@PRId, ProjectId=@ProjectId, Date=@Date, BidAnalysisId=@BidAnalysisId, BidderId=@BidderId, VendorId=@VendorId, Terms=@Terms, Clause=@Clause, Status=@Status, Currency=@Currency, ExchangeRate=@ExchangeRate, VendorName=@VendorName, VendorContact=@VendorContact, VendorTel=@VendorTel, VendorEmail=@VendorEmail, VendorAddress=@VendorAddress WHERE Id=@Id",
                        param, transaction);
                }

                var existingItemsInDb = connection.Query<POItem>("SELECT * FROM POItems WHERE POId = @Id", new { po.Id }, transaction).ToList();
                var currentItemIds = po.Items.Select(i => i.Id).ToList();

                // Delete items no longer present
                foreach (var existing in existingItemsInDb)
                {
                    if (!currentItemIds.Contains(existing.Id))
                    {
                        try
                        {
                            connection.Execute("DELETE FROM POItems WHERE Id = @Id", new { existing.Id }, transaction);
                        }
                        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
                        {
                            // Cannot delete as it's likely referenced in a GRN
                        }
                    }
                }

                foreach (var item in po.Items)
                {
                    item.POId = po.Id;
                    var itemParam = new {
                        item.Id, item.POId,
                        BudgetLineId = (item.BudgetLineId == null || item.BudgetLineId == 0) ? (int?)null : item.BudgetLineId,
                        item.Description, item.Unit, item.Quantity, item.UnitPrice
                    };

                    if (item.Id == 0)
                    {
                        item.Id = connection.QuerySingle<int>(
                            "INSERT INTO POItems (POId, BudgetLineId, Description, Unit, Quantity, UnitPrice) VALUES (@POId, @BudgetLineId, @Description, @Unit, @Quantity, @UnitPrice); SELECT last_insert_rowid();",
                            itemParam, transaction);
                    }
                    else
                    {
                        try
                        {
                            connection.Execute(
                                "UPDATE POItems SET BudgetLineId=@BudgetLineId, Description=@Description, Unit=@Unit, Quantity=@Quantity, UnitPrice=@UnitPrice WHERE Id=@Id",
                                itemParam, transaction);
                        }
                        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
                        {
                            // If referenced, we might still want to allow non-quantity/description updates if possible,
                            // but usually PO items shouldn't change after GRN.
                            // The user said "allow any updates", so maybe they mean header.
                            // If item itself is locked, SQLite won't allow UPDATE if it affects the FK in a child.
                            // But child (GRNItems) references POItemId. If POItemId doesn't change, UPDATE should be fine.
                            // UNLESS there is some other constraint.
                        }
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

        public void SavePR(PurchaseRequisition pr)
        {
            if (pr.ProjectId == 0) throw new Exception("Purchase Requisition must be linked to a valid Project (ProjectId is 0).");
            if (pr.RequesterId == 0) throw new Exception("Purchase Requisition must have a valid Requester (RequesterId is 0).");

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var projExists = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Projects WHERE Id = @ProjectId", new { pr.ProjectId }) > 0;
            if (!projExists) throw new Exception($"The linked Project (ID: {pr.ProjectId}) does not exist in the database.");

            var userExists = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Users WHERE Id = @RequesterId", new { pr.RequesterId }) > 0;
            if (!userExists) throw new Exception($"The requester (ID: {pr.RequesterId}) does not exist in the database.");

            connection.Execute("PRAGMA foreign_keys = ON;");
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
                }

                var existingItemsInDb = connection.Query<PRItem>("SELECT * FROM PRItems WHERE PRId = @Id", new { pr.Id }, transaction).ToList();
                var currentItemIds = pr.Items.Select(i => i.Id).ToList();

                foreach (var existing in existingItemsInDb)
                {
                    if (!currentItemIds.Contains(existing.Id))
                    {
                        try { connection.Execute("DELETE FROM PRItems WHERE Id = @Id", new { existing.Id }, transaction); }
                        catch (SqliteException ex) when (ex.SqliteErrorCode == 19) { }
                    }
                }

                foreach (var item in pr.Items)
                {
                    item.PRId = pr.Id;
                    var itemParam = new {
                        item.Id, item.PRId,
                        BudgetLineId = (item.BudgetLineId == 0) ? (int?)null : item.BudgetLineId,
                        item.Description, item.Unit, item.Quantity, item.UnitPrice
                    };

                    if (item.Id == 0)
                    {
                        item.Id = connection.QuerySingle<int>(
                            "INSERT INTO PRItems (PRId, BudgetLineId, Description, Unit, Quantity, UnitPrice) " +
                            "VALUES (@PRId, @BudgetLineId, @Description, @Unit, @Quantity, @UnitPrice); SELECT last_insert_rowid();", itemParam, transaction);
                    }
                    else
                    {
                        connection.Execute(
                            "UPDATE PRItems SET BudgetLineId=@BudgetLineId, Description=@Description, Unit=@Unit, Quantity=@Quantity, UnitPrice=@UnitPrice WHERE Id=@Id",
                            itemParam, transaction);
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
                "ContactInfo=@ContactInfo, Tel=@Tel, Email=@Email, LogoImage=@LogoImage, " +
                "PRTerms=@PRTerms, RFQTerms=@RFQTerms, POTerms=@POTerms WHERE Id=1", settings);
        }

        public decimal GetSpentBudget(int budgetLineId, int? excludePRId = null)
        {
            using var connection = new SqliteConnection(_connectionString);
            var budgetLine = connection.QuerySingleOrDefault<BudgetLine>("SELECT * FROM BudgetLines WHERE Id = @budgetLineId", new { budgetLineId });
            if (budgetLine == null) return 0;

            string sql = "SELECT pi.Quantity, pi.UnitPrice, pr.Currency, pr.ExchangeRate " +
                         "FROM PRItems pi JOIN PurchaseRequisitions pr ON pi.PRId = pr.Id " +
                         "WHERE pi.BudgetLineId = @budgetLineId AND pr.Status != 'Rejected'";
            
            if (excludePRId.HasValue)
            {
                sql += " AND pr.Id != @excludePRId";
            }

            var prItems = connection.Query<dynamic>(sql, new { budgetLineId, excludePRId });

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

        public decimal GetRemainingBudget(int budgetLineId, int? excludePRId = null)
        {
            using var connection = new SqliteConnection(_connectionString);
            var budgetLine = connection.QuerySingleOrDefault<BudgetLine>("SELECT * FROM BudgetLines WHERE Id = @budgetLineId", new { budgetLineId });
            if (budgetLine == null) return 0;
            
            return budgetLine.TotalAmount - GetSpentBudget(budgetLineId, excludePRId);
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
            connection.Execute("PRAGMA foreign_keys = ON;");
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
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                transaction.Rollback();
                CheckForeignKeys(connection);
                throw new Exception($"Database constraint violation in GRN: {ex.Message}", ex);
            }
            catch
            {
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
            connection.Execute("PRAGMA foreign_keys = ON;");
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
            connection.Open();
            connection.Execute("PRAGMA foreign_keys = ON;");
            try
            {
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
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                CheckForeignKeys(connection);
                throw new Exception($"Database constraint violation in Three-Way Match: {ex.Message}", ex);
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

        public IEnumerable<Vendor> GetVendors()
        {
            using var connection = new SqliteConnection(_connectionString);
            return connection.Query<Vendor>("SELECT * FROM Vendors WHERE IsActive = 1");
        }

        public void SaveVendor(Vendor vendor)
        {
            using var connection = new SqliteConnection(_connectionString);
            if (vendor.Id == 0)
            {
                vendor.Id = connection.QuerySingle<int>(
                    "INSERT INTO Vendors (Name, Address, Contact, Tel, Email, Category, TaxId, BankInfo, IsActive) " +
                    "VALUES (@Name, @Address, @Contact, @Tel, @Email, @Category, @TaxId, @BankInfo, @IsActive); SELECT last_insert_rowid();", vendor);
            }
            else
            {
                connection.Execute(
                    "UPDATE Vendors SET Name=@Name, Address=@Address, Contact=@Contact, Tel=@Tel, Email=@Email, " +
                    "Category=@Category, TaxId=@TaxId, BankInfo=@BankInfo, IsActive=@IsActive WHERE Id=@Id", vendor);
            }
        }

        public void DeleteVendor(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Execute("UPDATE Vendors SET IsActive = 0 WHERE Id = @id", new { id });
        }

        public IEnumerable<PRItem> GetPRItemsForRFQ(int rfqId)
        {
            using var connection = new SqliteConnection(_connectionString);
            return connection.Query<PRItem>(
                "SELECT pi.* FROM PRItems pi " +
                "JOIN RFQs r ON pi.PRId = r.PRId " +
                "WHERE r.Id = @rfqId", new { rfqId });
        }
    }
}
