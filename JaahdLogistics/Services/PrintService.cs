using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using JaahdLogistics.Views;

namespace JaahdLogistics.Services
{
    public class PrintService
    {
        public void ShowPreview(object dataContext, string templateName)
        {
            var preview = new PrintPreviewWindow(dataContext, templateName);
            preview.ShowDialog();
        }

        public void Print(FrameworkElement element)
        {
            PrintDialog printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                printDialog.PrintVisual(element, "Jaahd Logistics Print");
            }
        }
    }
}
