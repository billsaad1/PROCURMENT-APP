namespace JaahdLogistics.Models
{
    public class Settings
    {
        public int Id { get; set; } = 1;
        public string? AssociationName { get; set; }
        public byte[]? LogoImage { get; set; }
        public string? PRTerms { get; set; }
        public string? RFQTerms { get; set; }
        public string? POTerms { get; set; }
    }
}
