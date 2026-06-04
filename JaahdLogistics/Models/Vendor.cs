using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace JaahdLogistics.Models
{
    public partial class Vendor : ObservableObject
    {
        [ObservableProperty]
        private int _id;

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string? _address;

        [ObservableProperty]
        private string? _contact;

        [ObservableProperty]
        private string? _tel;

        [ObservableProperty]
        private string? _email;

        [ObservableProperty]
        private string? _category;

        [ObservableProperty]
        private string? _taxId;

        [ObservableProperty]
        private string? _bankInfo;

        [ObservableProperty]
        private bool _isActive = true;
    }
}
