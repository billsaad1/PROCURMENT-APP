using System;
using System.Collections.Generic;

namespace JaahdLogistics.Models
{
    public class RFQ : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
    {
        public int Id { get; set; }

        private string _rfqNumber = string.Empty;
        public string RFQNumber { get => _rfqNumber; set => SetProperty(ref _rfqNumber, value); }

        public int PRId { get; set; }

        private DateTime _date = DateTime.Now;
        public DateTime Date { get => _date; set => SetProperty(ref _date, value); }

        private DateTime? _closingDate;
        public DateTime? ClosingDate { get => _closingDate; set => SetProperty(ref _closingDate, value); }

        private string? _terms;
        public string? Terms { get => _terms; set => SetProperty(ref _terms, value); }
    }
}
