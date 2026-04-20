using System;
using System.Windows;

namespace JaahdLogistics
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnFlowDirectionChanged(object sender, EventArgs e)
        {
            // Optional: Handle layout updates on direction change if needed
        }
    }
}
