using System.Windows.Controls;

namespace JaahdLogistics.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
            DataContextChanged += (s, e) => {
                var proxy = Resources["ViewProxy"] as Helpers.BindingProxy;
                if (proxy != null) proxy.Data = DataContext;
            };
        }
    }
}
