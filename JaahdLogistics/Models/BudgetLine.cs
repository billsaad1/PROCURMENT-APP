namespace JaahdLogistics.Models
{
    public class BudgetLine
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal TotalAmount { get; set; }
        public string Currency { get; set; } = "USD";
    }
}
