using System;
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
            try
            {
                base.OnStartup(e);

                string connectionString = "Data Source=jaahd.db";
                var bootstrap = new DatabaseBootstrap(connectionString);
                bootstrap.Setup();

                var dataService = new DataService(connectionString);
                var langService = new LanguageService();
                var mainVM = new MainViewModel(dataService, langService);

                var mainWindow = new MainWindow();
                mainWindow.DataContext = mainVM;

                mainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Critical error during startup: {ex.Message}\n\n{ex.StackTrace}", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }
    }
}
