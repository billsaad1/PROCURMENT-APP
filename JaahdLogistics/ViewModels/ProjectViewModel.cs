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
                    lines.Select(l => new BudgetLineDisplay(l, _dataService.GetRemainingBudget(l.Id)))
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
            var newLine = new BudgetLine { ProjectId = SelectedProject.Id, Code = "1.1", TotalAmount = 0, Currency = "USD" };
            BudgetLines.Add(new BudgetLineDisplay(newLine, 0));
        }

        [RelayCommand]
        private void Save()
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
    }

    public partial class BudgetLineDisplay : ObservableObject
    {
        public BudgetLine Model { get; }

        [ObservableProperty]
        private decimal _remaining;

        public BudgetLineDisplay(BudgetLine model, decimal remaining)
        {
            Model = model;
            _remaining = remaining;
        }

        public string Code { get => Model.Code; set { Model.Code = value; OnPropertyChanged(); } }
        public string? Description { get => Model.Description; set { Model.Description = value; OnPropertyChanged(); } }
        public decimal TotalAmount { get => Model.TotalAmount; set { Model.TotalAmount = value; OnPropertyChanged(); } }
        public string Currency { get => Model.Currency; set { Model.Currency = value; OnPropertyChanged(); } }
        public decimal Spent => Model.TotalAmount - Remaining;
    }
}
