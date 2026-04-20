using System.Windows;
using System.Windows.Controls;

namespace JaahdLogistics.Views
{
    public partial class PrintPreviewWindow : Window
    {
        private readonly FrameworkElement _content;

        public PrintPreviewWindow(FrameworkElement content)
        {
            InitializeComponent();
            _content = content;
            PreviewContent.Content = content;
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            PrintDialog printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                printDialog.PrintVisual(_content, "Document Print");
            }
        }
    }
}
