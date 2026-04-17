using System;
using System.Collections.Generic;

namespace JaahdLogistics.Models
{
    public class RFQ
    {
        public int Id { get; set; }
        public string RFQNumber { get; set; } = string.Empty;
        public int PRId { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
        public DateTime? ClosingDate { get; set; }
        public string? Terms { get; set; }
    }
}
