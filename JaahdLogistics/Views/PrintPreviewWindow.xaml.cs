using System.Windows;
using System.Windows.Controls;

namespace JaahdLogistics.Views
{
    public partial class PrintPreviewWindow : Window
    {
        public PrintPreviewWindow(object dataContext, string templateName)
        {
            InitializeComponent();
            DataContext = dataContext;
            
            // Set the template based on the form type
            var template = Application.Current.TryFindResource(templateName) as ControlTemplate;
            if (template != null)
            {
                PreviewContent.Template = template;

                // Set landscape for Bid Analysis
                if (templateName == "BidAnalysisPrintTemplate")
                {
                    PreviewContent.Width = 1123;
                    PreviewContent.Height = 794;
                    PrintBorder.Width = 1123;
                    PrintBorder.Height = 794;
                    this.Width = 1200;
                }
            }
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            PrintDialog printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                // Print the border which contains the rendered template
                printDialog.PrintVisual(PrintBorder, "Document Print");
            }
        }
    }
}
