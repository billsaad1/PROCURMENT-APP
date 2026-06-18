namespace JaahdLogistics.Models
{
    public class Settings
    {
        public int Id { get; set; } = 1;
        public string? AssociationName { get; set; }
        public string? Address { get; set; }
        public string? ContactInfo { get; set; }
        public string? Tel { get; set; }
        public string? Email { get; set; }
        public byte[]? LogoImage { get; set; }
        public string? PRTerms { get; set; }
        public string? RFQTerms { get; set; }
        public string? POTerms { get; set; }
        public string? LogisticsManager { get; set; }
        public string? FinanceManager { get; set; }
        public string? HeadOfAssociation { get; set; }
        public string? LogisticsTitle { get; set; }
        public string? FinanceTitle { get; set; }
        public string? HeadTitle { get; set; }
        public int? DefaultLogisticsEmployeeId { get; set; }
        public int? DefaultFinanceEmployeeId { get; set; }
        public int? DefaultHeadEmployeeId { get; set; }
    }
}
