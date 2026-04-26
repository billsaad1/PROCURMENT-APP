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

                // Use a full path to ensure consistency across different startup methods
                // Cloud Ready: Load connection string from config or environment if available
                string dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "jaahd.db");
                string connectionString = System.Environment.GetEnvironmentVariable("JAAHD_CONNECTION_STRING")
                                         ?? $"Data Source={dbPath}";

                var bootstrap = new DatabaseBootstrap(connectionString);
                bootstrap.Setup();

                var dataService = new DataService(connectionString);
                var langService = new LanguageService();
                var mainVM = new MainViewModel(dataService, langService, connectionString);

                // Start background sync if cloud connection is available
                var syncService = new SyncService(dataService);
                _ = syncService.StartAutoSync();

                // Initialize the global binding proxy
                var proxy = Resources["Proxy"] as JaahdLogistics.Helpers.BindingProxy;
                if (proxy != null) proxy.Data = mainVM;

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
