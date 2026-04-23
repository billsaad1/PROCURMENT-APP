namespace JaahdLogistics.Models
{
    public class BudgetLine
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Unit { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalAmount { get; set; } // Settable for DB persistence
        public string Currency { get; set; } = "USD";
    }
}
