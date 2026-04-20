using System.Collections.Generic;
using JaahdLogistics.Models;

namespace JaahdLogistics.Services
{
    public interface IDataService
    {
        User? Authenticate(string username, string password);
        IEnumerable<User> GetUsers();
        void SaveUser(User user);
        IEnumerable<Project> GetProjects();
        void SaveProject(Project project);
        IEnumerable<PurchaseRequisition> GetPRs();
        IEnumerable<PurchaseOrder> GetPOs();
        void SavePR(PurchaseRequisition pr);
        void SaveRFQ(RFQ rfq);
        void SaveBidAnalysis(BidAnalysis analysis);
        void SavePO(PurchaseOrder po);
        void SaveBudgetLine(BudgetLine budgetLine);
        IEnumerable<BudgetLine> GetBudgetLines(int projectId);
        Settings GetSettings();
        void SaveSettings(Settings settings);
        decimal GetRemainingBudget(int budgetLineId);
        void ApproveEntity(string entityType, int entityId, int userId, string status);
        void SaveGRN(GoodsReceivingNotes grn, List<GRNItems> items);
    }
}
