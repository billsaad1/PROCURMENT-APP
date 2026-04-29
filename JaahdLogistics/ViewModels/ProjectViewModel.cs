using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JaahdLogistics.Models;
using JaahdLogistics.Services;

namespace JaahdLogistics.ViewModels
{
    public partial class ProjectViewModel : ViewModelBase
    {
        private readonly IDataService _dataService;

        [ObservableProperty]
        private ObservableCollection<Project> _projects;

        [ObservableProperty]
        private Project? _selectedProject;

        [ObservableProperty]
        private ObservableCollection<BudgetLineDisplay> _budgetLines = new();

        public ProjectViewModel(IDataService dataService)
        {
            _dataService = dataService;
            _projects = new ObservableCollection<Project>(_dataService.GetProjects());
        }

        partial void OnSelectedProjectChanged(Project? value)
        {
            if (value != null)
            {
                var lines = _dataService.GetBudgetLines(value.Id);
                BudgetLines = new ObservableCollection<BudgetLineDisplay>(
                    lines.Select(l => new BudgetLineDisplay(l, _dataService.GetSpentBudget(l.Id), _dataService.GetRemainingBudget(l.Id)))
                );
            }
            else
            {
                BudgetLines.Clear();
            }
        }

        [RelayCommand]
        private void AddProject()
        {
            var newProject = new Project { Name = "New Project", Code = "PROJ-" + (Projects.Count + 1), Year = System.DateTime.Now.Year };
            _dataService.SaveProject(newProject);
            Projects.Add(newProject);
        }

        [RelayCommand]
        private void AddBudgetLine()
        {
            if (SelectedProject == null) return;
            var newLine = new BudgetLine { ProjectId = SelectedProject.Id, Code = (BudgetLines.Count + 1).ToString(), TotalAmount = 0, Currency = "USD" };
            BudgetLines.Add(new BudgetLineDisplay(newLine, 0, 0));
        }

        [RelayCommand]
        private void Save()
        {
            try
            {
                if (SelectedProject == null) return;

                _dataService.SaveProject(SelectedProject);
                foreach (var line in BudgetLines)
                {
                    _dataService.SaveBudgetLine(line.Model);
                }

                MessageBox.Show("Project and Budget Lines saved successfully.");
                OnSelectedProjectChanged(SelectedProject); // Refresh to recalculate remaining
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving project: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void DeleteProject()
        {
            if (SelectedProject == null) return;
            var result = MessageBox.Show($"Are you sure you want to delete project '{SelectedProject.Name}' and all its budget lines?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                _dataService.DeleteProject(SelectedProject.Id);
                Projects.Remove(SelectedProject);
                SelectedProject = null;
            }
        }

        [RelayCommand]
        private void DeleteBudgetLine(BudgetLineDisplay line)
        {
            if (line == null) return;
            var result = MessageBox.Show($"Delete budget line '{line.Code}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    if (line.Model.Id > 0)
                    {
                        _dataService.DeleteBudgetLine(line.Model.Id);
                    }
                    BudgetLines.Remove(line);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Cannot delete budget line: {ex.Message}", "Delete Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    public partial class BudgetLineDisplay : ObservableObject
    {
        public BudgetLine Model { get; }

        [ObservableProperty]
        private decimal _spent;

        [ObservableProperty]
        private decimal _remaining;

        public BudgetLineDisplay(BudgetLine model, decimal spent, decimal remaining)
        {
            Model = model;
            _spent = spent;
            _remaining = remaining;
        }

        public string Code { get => Model.Code; set { Model.Code = value; OnPropertyChanged(); } }
        public string Name { get => Model.Name; set { Model.Name = value; OnPropertyChanged(); } }
        public string? Description { get => Model.Description; set { Model.Description = value; OnPropertyChanged(); } }
        public string? Unit { get => Model.Unit; set { Model.Unit = value; OnPropertyChanged(); } }

        public decimal Quantity
        {
            get => Model.Quantity;
            set { Model.Quantity = value; Model.TotalAmount = Model.Quantity * Model.UnitPrice; OnPropertyChanged(); OnPropertyChanged(nameof(TotalAmount)); UpdateRemaining(); }
        }

        public decimal UnitPrice
        {
            get => Model.UnitPrice;
            set { Model.UnitPrice = value; Model.TotalAmount = Model.Quantity * Model.UnitPrice; OnPropertyChanged(); OnPropertyChanged(nameof(TotalAmount)); UpdateRemaining(); }
        }

        public decimal TotalAmount => Model.TotalAmount;
        public string Currency { get => Model.Currency; set { Model.Currency = value; OnPropertyChanged(); } }

        private void UpdateRemaining()
        {
            Remaining = TotalAmount - Spent;
        }
    }
}
