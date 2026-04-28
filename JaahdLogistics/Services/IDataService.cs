using System.Collections.Generic;
using JaahdLogistics.Models;

namespace JaahdLogistics.Services
{
    public interface IDataService
    {
        User? Authenticate(string username, string password);
        IEnumerable<User> GetUsers();
        void SaveUser(User user);
        void DeleteUser(int id);
        IEnumerable<Project> GetProjects();
        void SaveProject(Project project);
        IEnumerable<PurchaseRequisition> GetPRs();
        void DeletePR(int id);
        IEnumerable<PurchaseOrder> GetPOs();
        void SavePR(PurchaseRequisition pr);
        void SaveRFQ(RFQ rfq);
        void SaveBidAnalysis(BidAnalysis analysis);
        void SavePO(PurchaseOrder po);
        void SaveBudgetLine(BudgetLine budgetLine);
        void DeleteBudgetLine(int id);
        void DeleteProject(int id);
        IEnumerable<BudgetLine> GetBudgetLines(int projectId);
        Settings GetSettings();
        void SaveSettings(Settings settings);
        decimal GetSpentBudget(int budgetLineId);
        decimal GetRemainingBudget(int budgetLineId);
        decimal GetLastExchangeRate(string currency);
        void ApproveEntity(string entityType, int entityId, int userId, string status);
        IEnumerable<dynamic> GetApprovals(string entityType, int entityId);
        void SaveGRN(GoodsReceivingNotes grn, List<GRNItems> items);
        IEnumerable<GoodsReceivingNotes> GetGRNs();
        void SaveThreeWayMatch(ThreeWayMatch match);
        IEnumerable<string> GetPreviousItemDescriptions();
    }
}
