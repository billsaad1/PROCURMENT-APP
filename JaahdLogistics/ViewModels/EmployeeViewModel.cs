using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JaahdLogistics.Models;
using JaahdLogistics.Services;
using Microsoft.Win32;
using System.IO;

namespace JaahdLogistics.ViewModels
{
    public partial class EmployeeViewModel : ViewModelBase
    {
        private readonly IDataService _dataService;

        [ObservableProperty]
        private ObservableCollection<Employee> _employees = new();

        [ObservableProperty]
        private Employee? _selectedEmployee;

        public EmployeeViewModel(IDataService dataService)
        {
            _dataService = dataService;
            LoadEmployees();
        }

        private void LoadEmployees()
        {
            Employees.Clear();
            foreach (var emp in _dataService.GetEmployees())
            {
                Employees.Add(emp);
            }
        }

        [RelayCommand]
        private void NewEmployee()
        {
            SelectedEmployee = new Employee();
            Employees.Add(SelectedEmployee);
        }

        [RelayCommand]
        private void SaveEmployee()
        {
            if (SelectedEmployee != null)
            {
                _dataService.SaveEmployee(SelectedEmployee);
                LoadEmployees();
            }
        }

        [RelayCommand]
        private void DeleteEmployee(Employee emp)
        {
            if (emp != null)
            {
                _dataService.DeleteEmployee(emp.Id);
                LoadEmployees();
            }
        }

        [RelayCommand]
        private void UploadSignature()
        {
            if (SelectedEmployee == null) return;

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                SelectedEmployee.SignatureImage = File.ReadAllBytes(openFileDialog.FileName);
            }
        }
    }
}
