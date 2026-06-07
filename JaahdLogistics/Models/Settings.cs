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
    }
}
