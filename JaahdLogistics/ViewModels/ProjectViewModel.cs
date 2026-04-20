using System.Collections.ObjectModel;
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
        private ObservableCollection<BudgetLine> _budgetLines = new();

        public ProjectViewModel(IDataService dataService)
        {
            _dataService = dataService;
            _projects = new ObservableCollection<Project>(_dataService.GetProjects());
        }

        partial void OnSelectedProjectChanged(Project? value)
        {
            if (value != null)
            {
                BudgetLines = new ObservableCollection<BudgetLine>(_dataService.GetBudgetLines(value.Id));
            }
            else
            {
                BudgetLines.Clear();
            }
        }

        [RelayCommand]
        private void AddProject()
        {
            var newProject = new Project { Name = "New Project", Code = "PROJ-" + (Projects.Count + 1), Year = DateTime.Now.Year };
            _dataService.SaveProject(newProject);
            Projects.Add(newProject);
        }

        [RelayCommand]
        private void AddBudgetLine()
        {
            if (SelectedProject == null) return;
            var newLine = new BudgetLine { ProjectId = SelectedProject.Id, Code = "1.1", TotalAmount = 0 };
            _dataService.SaveBudgetLine(newLine);
            BudgetLines.Add(newLine);
        }
    }
}
