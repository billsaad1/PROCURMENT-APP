using System;
using System.Collections.Generic;

namespace JaahdLogistics.Models
{
    public class GoodsReceivingNotes : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
    {
        public int Id { get; set; }
        public string GRNNumber { get; set; } = string.Empty;
        public int POId { get; set; }
        public DateTime Date { get; set; }
        public int ReceiverId { get; set; }

        private int? _receiverEmployeeId;
        public int? ReceiverEmployeeId { get => _receiverEmployeeId; set => SetProperty(ref _receiverEmployeeId, value); }

        public string Status { get; set; } = "Completed";

        private string? _invoiceNumber;
        public string? InvoiceNumber { get => _invoiceNumber; set => SetProperty(ref _invoiceNumber, value); }

        private bool _isQtyComply;
        public bool IsQtyComply { get => _isQtyComply; set => SetProperty(ref _isQtyComply, value); }

        private bool _isQtyMatch;
        public bool IsQtyMatch { get => _isQtyMatch; set => SetProperty(ref _isQtyMatch, value); }

        private bool _isQtyIntact;
        public bool IsQtyIntact { get => _isQtyIntact; set => SetProperty(ref _isQtyIntact, value); }

        public List<GRNItems> Items { get; set; } = new();
    }

    public class GRNItems
    {
        public int Id { get; set; }
        public int GRNId { get; set; }
        public int POItemId { get; set; }
        public string Description { get; set; } = string.Empty; // Added for UI
        public string Unit { get; set; } = string.Empty; // Added for UI
        public decimal OrderedQuantity { get; set; } // Added for UI
        public decimal ReceivedQuantity { get; set; }
        public decimal AcceptedQuantity { get; set; }
        public decimal RejectedQuantity { get; set; }
        public string? RejectReason { get; set; }
    }
}
