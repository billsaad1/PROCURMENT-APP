using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace JaahdLogistics.Models
{
    public partial class Employee : ObservableObject
    {
        public int Id { get; set; }

        [ObservableProperty]
        private string _nameEN = string.Empty;

        [ObservableProperty]
        private string _nameAR = string.Empty;

        [ObservableProperty]
        private string? _positionEN;

        [ObservableProperty]
        private string? _positionAR;

        [ObservableProperty]
        private byte[]? _signatureImage;

        public string DisplayName => $"{NameEN} / {NameAR}";
    }
}
