using JaahdLogistics.Models;

namespace JaahdLogistics.Services
{
    public class AuthService
    {
        public static User? CurrentUser { get; private set; }

        public static void Login(User user)
        {
            CurrentUser = user;
        }

        public static void Logout()
        {
            CurrentUser = null;
        }

        public static bool IsInRole(string role)
        {
            return CurrentUser?.Role == role;
        }

        public static bool CanApprovePR()
        {
            if (CurrentUser == null) return false;
            return CurrentUser.Role == "LogisticsManager" || 
                   CurrentUser.Role == "FinanceManager" || 
                   CurrentUser.Role == "ProjectManager" ||
                   CurrentUser.Role == "HeadOfAssociation" ||
                   CurrentUser.Role == "Admin";
        }
    }
}
