using System.Windows;
using JaahdLogistics.Services;
using JaahdLogistics.Data;
using JaahdLogistics.ViewModels;

namespace JaahdLogistics
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            string connectionString = "Data Source=jaahd.db";
            var bootstrap = new DatabaseBootstrap(connectionString);
            // bootstrap.Setup(); // Commented out because schema.sql is not in the build output yet

            var dataService = new DataService(connectionString);
            var langService = new LanguageService();
            var mainVM = new MainViewModel(dataService, langService);

            var mainWindow = new MainWindow();
            mainWindow.DataContext = mainVM;
            mainWindow.Show();
        }
    }
}
